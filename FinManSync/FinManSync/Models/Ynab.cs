
using System.Text.Json.Serialization;

namespace FinManSync.Models;

public class YnabPostSubtransaction
{
  [JsonPropertyName("amount")]
  public required int Amount { get; set; }

  [JsonPropertyName("payee_id")]
  public string PayeeId { get; set; } = string.Empty;

  [JsonPropertyName("payee_name")]
  public required string PayeeName { get; set; }

  [JsonPropertyName("category_id")]
  public string CategoryId { get; set; } = string.Empty;

  [JsonPropertyName("memo")]
  public string Memo { get; set; } = string.Empty;
}

public class YnabPostTransaction
{
  [JsonPropertyName("account_id")]
  public required string AccountId { get; set; }

  [JsonPropertyName("date")]
  public required string Date { get; set; }

  [JsonPropertyName("amount")]
  public required int Amount { get; set; }

  [JsonPropertyName("payee_id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? PayeeId { get; set; }

  [JsonPropertyName("payee_name")]
  public required string PayeeName { get; set; }

  [JsonPropertyName("category_id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? CategoryId { get; set; }

  [JsonPropertyName("memo")]
  public string Memo { get; set; } = string.Empty;

  [JsonPropertyName("cleared")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Cleared { get; set; }

  [JsonPropertyName("approved")]
  public bool Approved { get; set; }

  [JsonPropertyName("flag_color")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? FlagColor { get; set; }

  [JsonPropertyName("subtransactions")]
  public List<YnabPostSubtransaction> Subtransactions { get; set; } = [];

  [JsonPropertyName("import_id")]
  public required string ImportId { get; set; }
}

public class YnabPostTransactionRequest
{
  [JsonPropertyName("transaction")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public YnabPostTransaction? Transaction { get; set; }

  [JsonPropertyName("transactions")]
  public List<YnabPostTransaction> Transactions { get; set; } = [];
}

public class YnabPatchSubtransaction
{
  [JsonPropertyName("amount")]
  public required int Amount { get; set; }

  [JsonPropertyName("payee_id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? PayeeId { get; set; }

  [JsonPropertyName("payee_name")]
  public required string PayeeName { get; set; }

  [JsonPropertyName("category_id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? CategoryId { get; set; }

  [JsonPropertyName("memo")]
  public string Memo { get; set; } = string.Empty;
}

public class YnabPatchTransaction
{
  [JsonPropertyName("id")]
  public string Id { get; set; } = string.Empty;

  [JsonPropertyName("import_id")]
  public required string ImportId { get; set; }

  [JsonPropertyName("account_id")]
  public required string AccountId { get; set; }

  [JsonPropertyName("date")]
  public required string Date { get; set; }

  [JsonPropertyName("amount")]
  public required int Amount { get; set; }

  [JsonPropertyName("payee_id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? PayeeId { get; set; }

  [JsonPropertyName("payee_name")]
  public required string PayeeName { get; set; }

  [JsonPropertyName("category_id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? CategoryId { get; set; }

  [JsonPropertyName("memo")]
  public string Memo { get; set; } = string.Empty;

  [JsonPropertyName("cleared")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Cleared { get; set; }

  [JsonPropertyName("approved")]
  public bool Approved { get; set; }

  [JsonPropertyName("flag_color")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? FlagColor { get; set; }

  [JsonPropertyName("subtransactions")]
  public List<YnabPatchSubtransaction> Subtransactions { get; set; } = [];
}

public class YnabPatchTransactionRequest
{
  [JsonPropertyName("transactions")]
  public List<YnabPatchTransaction> Transactions { get; set; } = [];
}