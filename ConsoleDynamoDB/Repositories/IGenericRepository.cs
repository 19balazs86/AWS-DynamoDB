using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public interface IGenericRepository<TEntity> where TEntity : IEntity
{
    Task<bool> CreateItem(TEntity entity);

    Task<TEntity?> GetItem(string partitionKey, string sortKey);

    Task<TEntity[]> GetItems(string partitionKey);

    Task<TEntity[]> GetItemsBySortKeyPrefix(string partitionKey, string sortKeyPrefix);

    Task<TEntity[]> GetAllAsync();

    Task<bool> UpdateItem(TEntity entity);

    Task<bool> DeleteItem(string partitionKey, string sortKey);
}
