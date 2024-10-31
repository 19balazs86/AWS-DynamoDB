using System.Text.Json.Serialization;

namespace ConsoleDynamoDB.Entities;

public sealed class Comment : IEntity
{
    public static string TableName => "Comments";

    [JsonPropertyName("pk")]
    public string Pk => BlogPostId.ToString();

    [JsonPropertyName("sk")]
    public string Sk => $"{UserId}#{Id}"; // // Composite key: UserId#CommentId

    public Guid   Id   { get; init; }
    public string Text { get; set; } = string.Empty;

    public Guid BlogPostId { get; init; }
    public Guid UserId     { get; init; }
}