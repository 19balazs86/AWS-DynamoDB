namespace ConsoleDynamoDB.Entities;

public interface IEntity
{
    public static abstract string TableName { get; }

    public string Pk { get; }
    public string Sk { get; }

    public Guid Id { get; init; }
}
