using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public class GenericRepository<TEntity>(IAmazonDynamoDB _dynamoDb) : IGenericRepository<TEntity> where TEntity : IEntity
{
    public async Task<bool> CreateItem(TEntity entity)
    {
        Dictionary<string, AttributeValue> entityAttributeMap = entityToAttributeValues(entity);

        // The default behavior of PutItemRequest is to create or update
        // The ConditionExpression ensures that only the create operation is allowed
        var createItemRequest = new PutItemRequest
        {
            TableName           = TEntity.TableName,
            Item                = entityAttributeMap,
            ConditionExpression = "attribute_not_exists(pk) and attribute_not_exists(sk)"
        };

        try
        {
            PutItemResponse? response = await _dynamoDb.PutItemAsync(createItemRequest);

            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<TEntity?> GetItem(string partitionKey, string sortKey)
    {
        var keyAttributeValues = new Dictionary<string, AttributeValue>
        {
            { "pk", new AttributeValue(partitionKey) },
            { "sk", new AttributeValue(sortKey) }
        };

        var getItemRequest = new GetItemRequest(TEntity.TableName, keyAttributeValues);

        GetItemResponse response = await _dynamoDb.GetItemAsync(getItemRequest);

        return attributeValueToEntity(response.Item);
    }

    public async Task<TEntity[]> GetItems(string partitionKey)
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":pk", new AttributeValue(partitionKey) }
        };

        var queryRequest = new QueryRequest
        {
            TableName                 = TEntity.TableName,
            KeyConditionExpression    = "pk = :pk",
            ExpressionAttributeValues = expressionAttributeValues
        };

        QueryResponse response = await _dynamoDb.QueryAsync(queryRequest);

        return response.Items.Select(attributeValueToEntity).ToArray()!;
    }

    public async Task<TEntity[]> GetItemsBySortKeyPrefix(string partitionKey, string sortKeyPrefix)
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":pk",       new AttributeValue(partitionKey) },
            { ":skPrefix", new AttributeValue(sortKeyPrefix) }
        };

        var queryRequest = new QueryRequest
        {
            TableName                 = TEntity.TableName,
            KeyConditionExpression    = "pk = :pk AND begins_with(sk, :skPrefix)",
            ExpressionAttributeValues = expressionAttributeValues
        };

        QueryResponse response = await _dynamoDb.QueryAsync(queryRequest);

        return response.Items.Select(attributeValueToEntity).ToArray()!;
    }

    public async Task<TEntity[]> GetAllAsync()
    {
        // The scan operation is not recommended because it requires a large amount of resources due to the nature of scanning all partitions
        var scanRequest = new ScanRequest(TEntity.TableName);

        ScanResponse response = await _dynamoDb.ScanAsync(scanRequest);

        return response.Items.Select(attributeValueToEntity).ToArray()!;
    }

    public async Task<bool> UpdateItem(TEntity entity)
    {
        Dictionary<string, AttributeValue> entityAttributeMap = entityToAttributeValues(entity);

        // The default behavior of PutItemRequest is to create or update
        // The ConditionExpression ensures that only the update operation is allowed
        var updateItemRequest = new PutItemRequest
        {
            TableName           = TEntity.TableName,
            Item                = entityAttributeMap,
            ConditionExpression = "attribute_exists(pk) and attribute_exists(sk)"
        };

        try
        {
            PutItemResponse? response = await _dynamoDb.PutItemAsync(updateItemRequest);

            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteItem(string partitionKey, string sortKey)
    {
        var keyAttributeValues = new Dictionary<string, AttributeValue>
        {
            { "pk", new AttributeValue(partitionKey) },
            { "sk", new AttributeValue(sortKey) }
        };

        var deletedItemRequest = new DeleteItemRequest
        {
            TableName = TEntity.TableName,
            Key       = keyAttributeValues
        };

        DeleteItemResponse response = await _dynamoDb.DeleteItemAsync(deletedItemRequest);

        // Without a ConditionExpression, the response is OK, even if there was no entity to delete
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    private static Dictionary<string, AttributeValue> entityToAttributeValues(TEntity entity)
    {
        string serializedEntity = JsonSerializer.Serialize(entity);

        return Document.FromJson(serializedEntity).ToAttributeMap();
    }

    private static TEntity? attributeValueToEntity(Dictionary<string, AttributeValue>? attributeValues)
    {
        if (attributeValues is null || attributeValues.Count == 0)
        {
            return default;
        }

        Document document = Document.FromAttributeMap(attributeValues);

        return JsonSerializer.Deserialize<TEntity>(document.ToJson());
    }
}