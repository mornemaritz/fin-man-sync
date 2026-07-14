
// Model classes matching the JSON structure
using System.Text.Json.Serialization;

namespace FinManSync.Models;

public class Transaction
{
    [JsonPropertyName("accountId")]
    [JsonRequired]
    public required string AccountId { get; set; }
    [JsonPropertyName("type")]
    [JsonRequired]
    public required string Type { get; set; }
    [JsonPropertyName("transactionType")]
    [JsonRequired]
    public required string TransactionType { get; set; }
    [JsonPropertyName("status")]
    [JsonRequired]
    public required string Status { get; set; }
    [JsonPropertyName("description")]
    [JsonRequired]
    public required string Description { get; set; }
    [JsonPropertyName("cardNumber")]
    [JsonRequired]
    public required string CardNumber { get; set; }
    [JsonPropertyName("postedOrder")]
    [JsonRequired]
    public required int PostedOrder { get; set; }
    [JsonPropertyName("postingDate")]
    [JsonRequired]
    public required string PostingDate { get; set; }
    [JsonPropertyName("valueDate")]
    [JsonRequired]
    public required string ValueDate { get; set; }
    [JsonPropertyName("actionDate")]
    [JsonRequired]
    public required string ActionDate { get; set; }
    [JsonPropertyName("transactionDate")]
    [JsonRequired]
    public required string TransactionDate { get; set; }
    [JsonPropertyName("amount")]
    [JsonRequired]
    public required double Amount { get; set; }
    [JsonPropertyName("runningBalance")]
    [JsonRequired]
    public required double RunningBalance { get; set; }
    [JsonPropertyName("uuid")]
    [JsonRequired]
    public required string UUID { get; set; }
    public double SignedAmount => Type.Equals("credit", StringComparison.OrdinalIgnoreCase) ? Amount : -Amount;
}

public class Data
{
  [JsonPropertyName("transactions")]
  public List<Transaction> Transactions { get; set; } = [];
}

public class TransactionResponse
{
  [JsonPropertyName("data")]
  public Data Data { get; set; } = new();
}

public class InvestecTokenResponse
{
  [JsonPropertyName("access_token")]
  public required string AccessToken { get; set; }

  [JsonPropertyName("token_type")]
  public required string TokenType { get; set; }

  [JsonPropertyName("expires_in")]
  public required int ExpiresIn { get; set; }

  [JsonPropertyName("scope")]
  public required string Scope { get; set; }
}