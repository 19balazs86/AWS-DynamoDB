using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public interface IGenericRepository<TEntity> where TEntity : IEntity
{
    Task<bool> AddItem(TEntity entity);

    Task<TEntity?> GetItemById(string partitionKey, string sortKey);

    Task<TEntity[]> GetItemsByPartition(string partitionKey);

    Task<TEntity[]> GetItemsUsingIndex(string partitionKey, string lsiKey);

    Task<TEntity[]> GetItemsBySortKeyPrefix(string partitionKey, string sortKeyPrefix);

    Task<TEntity[]> GetItemsByScaning();

    Task<bool> UpdateItem(TEntity entity);

    Task<bool> DeleteItem(string partitionKey, string sortKey);
}
