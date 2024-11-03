namespace ConsoleDynamoDB.Types;

public sealed class PageResult<TEntity> where TEntity : class
{
    public List<TEntity> Items { get; } = [];

    public string? ContinuationToken { get; }

    public bool HasMoreItems => !string.IsNullOrWhiteSpace(ContinuationToken);

    public static PageResult<TEntity> Empty => new();

    private PageResult()
    {

    }

    public PageResult(IEnumerable<TEntity> items, string? continuationToken)
    {
        Items = [..items];

        ContinuationToken = continuationToken;
    }
}
