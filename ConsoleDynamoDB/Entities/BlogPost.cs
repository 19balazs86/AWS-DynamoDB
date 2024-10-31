using System.Text.Json.Serialization;

namespace ConsoleDynamoDB.Entities;

public sealed class BlogPost : IEntity
{
    public static string TableName => "BlogPosts";

    [JsonPropertyName("pk")]
    public string Pk => TenantId.ToString();

    [JsonPropertyName("sk")]
    public string Sk => $"{UserId}#{Id}"; // Composite key: UserId#PostId

    public Guid   Id      { get; init; }
    public string Title   { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Rating Rating  { get; set; } = new();

    public Guid UserId   { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class Rating
{
    public int Sum   { get; set; }
    public int Count { get; set; }
    public int Avg   { get; set; }
}
