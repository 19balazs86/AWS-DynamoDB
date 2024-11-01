using System.Text.Json.Serialization;

namespace ConsoleDynamoDB.Entities;

public sealed class User : IEntity
{
    public static string TableName => "Users";

    [JsonPropertyName("pk")]  public string Pk  => TenantId.ToString();
    [JsonPropertyName("sk")]  public string Sk  => Id.ToString();
    [JsonPropertyName("lsi")] public string Lsi => string.Empty; // Not used in this table

    public Guid   Id       { get; init; }
    public string Name     { get; set; } = string.Empty;
    public Guid   TenantId { get; init; }
}
