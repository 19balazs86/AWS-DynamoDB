using ConsoleDynamoDB.Entities;
using ConsoleDynamoDB.Types;

namespace ConsoleDynamoDB.Repositories;

public interface IGenericRepository<TEntity> where TEntity : class, IEntity
{
    Task<bool> AddItem(TEntity entity);

    Task<TEntity?> GetItemById(string partitionKey, string sortKey);

    Task<TEntity[]> GetItemsByPartition(string partitionKey);

    Task<PageResult<TEntity>> GetPagedItems(PageQuery pageQuery);

    Task<TEntity[]> GetItemsUsingIndex(string partitionKey, string lsiKey);

    Task<TEntity[]> GetItemsBySortKeyPrefix(string partitionKey, string sortKeyPrefix);

    Task<TEntity[]> GetItemsByScanning();

    Task<List<(string PartitionKey, string SortKey)>> GetKeysByScanning();

    Task<bool> UpdateItem(TEntity entity);

    Task<bool> DeleteItem(string partitionKey, string sortKey);

    Task<int> CountItems(string partitionKey);
}
