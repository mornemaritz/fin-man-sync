using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinManSync.Models;
using Microsoft.Extensions.Configuration;

namespace FinManSync;

public class Operator
{
  readonly string investecTransactionGetUrl;
  readonly string investecTokenUrl = "https://openapi.investec.com/identity/v2/oauth2/token";
  readonly string ynabTransactionPostUrl;
  private readonly string _investecClientId;
  private readonly string _investecClientSecret;
  private readonly string _investecApiKey;
  private readonly string _investecAccountId;
  private readonly string _ynabBearerToken;
  private readonly string _ynabBudgetId;
  private readonly string _ynabAccountId;

  public Operator(IConfiguration configuration)
  {
    _investecClientId = configuration["INVESTEC_CLIENT_ID"] ?? throw new Exception("INVESTEC_CLIENT_ID environment variable is not set.");
    _investecClientSecret = configuration["INVESTEC_CLIENT_SECRET"] ?? throw new Exception("INVESTEC_CLIENT_SECRET environment variable is not set.");
    _investecApiKey = configuration["INVESTEC_API_KEY"] ?? throw new Exception("INVESTEC_API_KEY environment variable is not set.");
    _investecAccountId = configuration["INVESTEC_ACCOUNT_ID"] ?? throw new Exception("INVESTEC_ACCOUNT_ID environment variable is not set.");

    _ynabBearerToken = configuration["YNAB_BEARER_TOKEN"] ?? throw new Exception("YNAB_BEARER_TOKEN environment variable is not set.");
    _ynabBudgetId = configuration["YNAB_BUDGET_ID"] ?? throw new Exception("YNAB_BUDGET_ID environment variable is not set.");
    _ynabAccountId = configuration["YNAB_ACCOUNT_ID"] ?? throw new Exception("YNAB_ACCOUNT_ID environment variable is not set.");

    investecTransactionGetUrl = $"https://openapi.investec.com/za/pb/v1/accounts/{_investecAccountId}/transactions";
    ynabTransactionPostUrl = $"https://api.ynab.com/v1/budgets/{_ynabBudgetId}/transactions";
  }

  public string GetInvestecAuthToken()
  {
    var request = new HttpRequestMessage
    {
      Method = HttpMethod.Post,
      RequestUri = new Uri(investecTokenUrl),
      Headers =
          {
              { "Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_investecClientId}:{_investecClientSecret}"))}" },
              { "x-api-key", _investecApiKey },
          },
      Content = new FormUrlEncodedContent(new Dictionary<string, string>
          {
              { "grant_type", "client_credentials" },
          }),
    };

    using var client = new HttpClient();
    using var response = client.Send(request);

    response.EnsureSuccessStatusCode();
    var body = response.Content.ReadAsStringAsync().Result;
    var tokenResponse = JsonSerializer.Deserialize<InvestecTokenResponse>(body);

    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
    {
      throw new Exception("Failed to retrieve Investec access token.");
    }

    return tokenResponse.AccessToken;
  }

  public async Task<string> OperateAsync(string[]? args)
  {
    var lastSyncDateFilePath = $"{Environment.CurrentDirectory}/LAST_SYNC_DATE";
    var lastSyncDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

    var toSyncDate = DateTime.TryParseExact(
        args?.Length > 0 ? args[0] : DateTime.Now.ToString("yyyy-MM-dd"), "yyyy-MM-dd",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var parsedDate
      )
      ? parsedDate.ToString("yyyy-MM-dd")
      : DateTime.Now.ToString("yyyy-MM-dd");

    if (File.Exists(lastSyncDateFilePath) && File.ReadAllText(lastSyncDateFilePath) is string lastSyncDateFromFile && !string.IsNullOrEmpty(lastSyncDateFromFile))
    {
      lastSyncDate = lastSyncDateFromFile;
      Console.WriteLine($"Using last sync date from file: {lastSyncDateFromFile}");
    }
    else
    {
      Console.WriteLine($"{lastSyncDateFilePath} not found or is empty. Using yesterday's date.");
    }

    Console.WriteLine($"Using toSyncDate: {toSyncDate}");

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetInvestecAuthToken()}");
    httpClient.DefaultRequestHeaders.Add("x-api-key", _investecApiKey );

    string fromDate = lastSyncDate;
    string toDate = toSyncDate;

    var investectTransactionResponse = await httpClient.GetFromJsonAsync<TransactionResponse>($"{investecTransactionGetUrl}?fromDate={fromDate}&toDate={toDate}");

    var ynabPostTransactions = investectTransactionResponse?.Data.Transactions.Select(t => new YnabPostTransaction
    {
      AccountId = _ynabAccountId,
      Date = t.PostingDate,
      Amount = (int)(t.SignedAmount * 1000), // Convert to milliunits
      PayeeName = t.Description,
      ImportId = t.UUID // Using uuid as ImportId

    }).ToList();

    if (ynabPostTransactions?.Count != 0)
    {
      var ynabTransactionRoot = new YnabPostTransactionRequest
      {
        Transactions = ynabPostTransactions!
      };

      httpClient.DefaultRequestHeaders.Clear();
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_ynabBearerToken}");

      var postResponse = await httpClient.PostAsJsonAsync(ynabTransactionPostUrl, ynabTransactionRoot);
      postResponse.EnsureSuccessStatusCode();

      File.WriteAllText(lastSyncDateFilePath, toSyncDate);
    }

    return "Done";
  }
}