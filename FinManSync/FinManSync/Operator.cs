using System.Globalization;
using System.Net.Http.Headers;
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

  private static string GetRequiredConfigValue(IConfiguration configuration, string key)
  {
    var rawValue = configuration[key] ?? throw new Exception($"{key} environment variable is not set.");
    var normalizedValue = rawValue.Trim().Trim('"');

    if (string.IsNullOrWhiteSpace(normalizedValue))
    {
      throw new Exception($"{key} environment variable is empty.");
    }

    return normalizedValue;
  }

  public Operator(IConfiguration configuration)
  {
    Console.WriteLine("Initializing Operator with configuration...");
    Console.WriteLine($"INVESTEC_CLIENT_ID: {(string.IsNullOrEmpty(configuration["INVESTEC_CLIENT_ID"]) ? "Not Set" : "Set")}");
    _investecClientId = GetRequiredConfigValue(configuration, "INVESTEC_CLIENT_ID");
    Console.WriteLine($"INVESTEC_CLIENT_SECRET: {(string.IsNullOrEmpty(configuration["INVESTEC_CLIENT_SECRET"]) ? "Not Set" : "Set")}");
    _investecClientSecret = GetRequiredConfigValue(configuration, "INVESTEC_CLIENT_SECRET");
    Console.WriteLine($"INVESTEC_API_KEY: {(string.IsNullOrEmpty(configuration["INVESTEC_API_KEY"]) ? "Not Set" : "Set")}");
    _investecApiKey = GetRequiredConfigValue(configuration, "INVESTEC_API_KEY");
    Console.WriteLine($"INVESTEC_ACCOUNT_ID: {(string.IsNullOrEmpty(configuration["INVESTEC_ACCOUNT_ID"]) ? "Not Set" : "Set")}");
    _investecAccountId = GetRequiredConfigValue(configuration, "INVESTEC_ACCOUNT_ID");

    Console.WriteLine($"YNAB_BEARER_TOKEN: {(string.IsNullOrEmpty(configuration["YNAB_BEARER_TOKEN"]) ? "Not Set" : "Set")}");
    _ynabBearerToken = GetRequiredConfigValue(configuration, "YNAB_BEARER_TOKEN");
    Console.WriteLine($"YNAB_BUDGET_ID: {(string.IsNullOrEmpty(configuration["YNAB_BUDGET_ID"]) ? "Not Set" : "Set")}");
    _ynabBudgetId = GetRequiredConfigValue(configuration, "YNAB_BUDGET_ID");
    Console.WriteLine($"YNAB_ACCOUNT_ID: {(string.IsNullOrEmpty(configuration["YNAB_ACCOUNT_ID"]) ? "Not Set" : "Set")}");
    _ynabAccountId = GetRequiredConfigValue(configuration, "YNAB_ACCOUNT_ID");

    investecTransactionGetUrl = $"https://openapi.investec.com/za/pb/v1/accounts/{_investecAccountId}/transactions";
    ynabTransactionPostUrl = $"https://api.ynab.com/v1/plans/{_ynabBudgetId}/transactions";
  }

  public string GetInvestecAuthToken()
  {
    Console.WriteLine("Base64 Encoded Client ID and Secret: " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_investecClientId}:{_investecClientSecret}")));

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

    Console.WriteLine($"Investec token response status code: {response.StatusCode}");
    Console.WriteLine($"Investec token response message: {response.ReasonPhrase}");
    var body = response.Content.ReadAsStringAsync().Result;
    Console.WriteLine($"Investec token response body: {body}");

    response.EnsureSuccessStatusCode();
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

    if (File.Exists(lastSyncDateFilePath) && File.ReadAllText(lastSyncDateFilePath) is string lastSyncDateFromFile && !string.IsNullOrWhiteSpace(lastSyncDateFromFile))
    {
      Console.WriteLine($"Using last sync date from file: {lastSyncDateFilePath}"); 
      lastSyncDate = lastSyncDateFromFile.Trim();
      Console.WriteLine($"Using last sync date from file: {lastSyncDate}");
    }
    else
    {
      Console.WriteLine($"{lastSyncDateFilePath} not found or is empty. Using yesterday's date.");
    }

    Console.WriteLine($"Using toSyncDate: {toSyncDate}");

    using var httpClient = new HttpClient();

    string fromDate = lastSyncDate.Trim();
    string toDate = toSyncDate.Trim();

    var investecRequestUri = $"{investecTransactionGetUrl}?fromDate={Uri.EscapeDataString(fromDate)}&toDate={Uri.EscapeDataString(toDate)}";
    using var investecRequest = new HttpRequestMessage(HttpMethod.Get, investecRequestUri);
    investecRequest.Version = new Version(1, 1);
    investecRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetInvestecAuthToken());
    investecRequest.Headers.Add("x-api-key", _investecApiKey);
    investecRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    investecRequest.Headers.UserAgent.ParseAdd("FinManSync/1.0");

    var investectTransactionResponse = await httpClient.SendAsync(investecRequest);
    ArgumentNullException.ThrowIfNull(investectTransactionResponse);

    Console.WriteLine($"Investec transaction response status code: {investectTransactionResponse.StatusCode}");
    Console.WriteLine($"Investec transaction response message: {investectTransactionResponse.ReasonPhrase}");
    var body = investectTransactionResponse.Content is null
      ? string.Empty
      : await investectTransactionResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"Investec transaction response body: {body}");

    if (!investectTransactionResponse.IsSuccessStatusCode)
    {
      return $"Investec transaction API returned an error. Status code: {(int)investectTransactionResponse.StatusCode}, Reason: {investectTransactionResponse.ReasonPhrase}, Body: {body}";
    }

    var ynabPostTransactions = JsonSerializer.Deserialize<TransactionResponse>(body)?.Data.Transactions.Select(t => new YnabPostTransaction
    {
      AccountId = _ynabAccountId,
      Date = t.TransactionDate,
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
      if (!postResponse.IsSuccessStatusCode)
      {
        var postResponseBody = postResponse.Content is null
          ? string.Empty
          : await postResponse.Content.ReadAsStringAsync();
        return $"YNAB transaction API returned an error. Status code: {(int)postResponse.StatusCode}, Reason: {postResponse.ReasonPhrase}, Body: {postResponseBody}";
      }

      File.WriteAllText(lastSyncDateFilePath, toSyncDate);
    }

    return "Sync job completed successfully.";
  }
}