namespace ConsoleDynamoDB.Types;

public sealed class PageQuery
{
    private readonly int _pageSize;

    public int PageSize
    {
        get  => _pageSize;
        init => _pageSize = value is <= 0 or > PageQueryDefaults.PageSizeMax ? PageQueryDefaults.PageSizeDefault : value;
    }

    public string? ContinuationToken { get; set; }

    public string PartitionKey { get; init; } = string.Empty;
}

public static class PageQueryDefaults
{
    public const int PageSizeDefault = 20;
    public const int PageSizeMax     = 50;
}
