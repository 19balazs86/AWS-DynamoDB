using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;
using ConsoleDynamoDB.Types;

namespace ConsoleDynamoDB.Repositories;

public class GenericRepository<TEntity>(IAmazonDynamoDB _dynamoDb) : IGenericRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly IAmazonDynamoDB _dynamoDb = _dynamoDb;

    public async Task<bool> AddItem(TEntity entity)
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

    public async Task<TEntity?> GetItemById(string partitionKey, string sortKey)
    {
        Dictionary<string, AttributeValue> keyAttributeValues = getPkSkAttributeValues(partitionKey, sortKey);

        var getItemRequest = new GetItemRequest(TEntity.TableName, keyAttributeValues);

        GetItemResponse response = await _dynamoDb.GetItemAsync(getItemRequest);

        return attributeValuesToEntity(response.Item);
    }

    public async Task<TEntity[]> GetItemsByPartition(string partitionKey)
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_Pk", new AttributeValue(partitionKey) }
        };

        var queryRequest = new QueryRequest
        {
            TableName                 = TEntity.TableName,
            KeyConditionExpression    = "pk = :v_Pk",
            ExpressionAttributeValues = expressionAttributeValues
        };

        QueryResponse response = await _dynamoDb.QueryAsync(queryRequest);

        return response.Items.Select(attributeValuesToEntity).ToArray()!;
    }

    public async Task<PageResult<TEntity>> GetPagedItems(PageQuery pageQuery)
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_Pk", new AttributeValue(pageQuery.PartitionKey) }
        };

        Dictionary<string, AttributeValue>? exclusiveStartKey = continuationTokenToExclusiveStartKey(pageQuery.ContinuationToken);

        var queryRequest = new QueryRequest
        {
            TableName                 = TEntity.TableName,
            KeyConditionExpression    = "pk = :v_Pk",
            ExpressionAttributeValues = expressionAttributeValues,
            Limit                     = pageQuery.PageSize,
            ExclusiveStartKey         = exclusiveStartKey,
            // The default value is true, representing Ascendant order. If you use "sk" as Ulid text or an Ulid-generated GUID, it can be ordered
            // ScanIndexForward          = false
        };

        QueryResponse response = await _dynamoDb.QueryAsync(queryRequest);

        if (response.Items.Count == 0)
        {
            return PageResult<TEntity>.Empty;
        }

        string? continuationToken = lastEvaluatedKeyToContinuationToken(response.LastEvaluatedKey);

        List<TEntity> items = response.Items.Select(attributeValuesToEntity).ToList()!;

        return new PageResult<TEntity>(items, continuationToken);
    }

    public async Task<TEntity[]> GetItemsUsingIndex(string partitionKey, string lsiKey)
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_Pk",  new AttributeValue(partitionKey) },
            { ":v_lsi", new AttributeValue(lsiKey) }
        };

        var queryRequest = new QueryRequest
        {
            TableName                 = TEntity.TableName,
            IndexName                 = InfrastructureRepository.GetLocalSecondaryIndexName<TEntity>(),
            KeyConditionExpression    = "pk = :v_Pk and lsi = :v_lsi",
            ExpressionAttributeValues = expressionAttributeValues
        };

        QueryResponse response = await _dynamoDb.QueryAsync(queryRequest);

        return response.Items.Select(attributeValuesToEntity).ToArray()!;
    }

    public async Task<TEntity[]> GetItemsBySortKeyPrefix(string partitionKey, string sortKeyPrefix)
    {
        // You can create a sort key similar to a composite key, such as UserId#BlogPostId or UserId#CommentId
        // Then, you can use the begins_with expression to filter the sort key by UserId

        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_Pk",       new AttributeValue(partitionKey) },
            { ":v_SkPrefix", new AttributeValue(sortKeyPrefix) }
        };

        var queryRequest = new QueryRequest
        {
            TableName                 = TEntity.TableName,
            KeyConditionExpression    = "pk = :v_Pk AND begins_with(sk, :v_SkPrefix)",
            ExpressionAttributeValues = expressionAttributeValues
        };

        QueryResponse response = await _dynamoDb.QueryAsync(queryRequest);

        return response.Items.Select(attributeValuesToEntity).ToArray()!;
    }

    public async Task<TEntity[]> GetItemsByScanning()
    {
        // The scan operation is not recommended because it requires a large amount of resources due to the nature of scanning all partitions
        // TODO: Use pagination with Limit and LastEvaluatedKey as in GetPagedItems method
        var scanRequest = new ScanRequest(TEntity.TableName);

        ScanResponse response = await _dynamoDb.ScanAsync(scanRequest);

        return response.Items.Select(attributeValuesToEntity).ToArray()!;
    }

    public async Task<List<(string PartitionKey, string SortKey)>> GetKeysByScanning()
    {
        // The scan operation is not recommended because it requires a large amount of resources due to the nature of scanning all partitions
        // TODO: Use pagination with Limit and LastEvaluatedKey as in GetPagedItems method
        var scanRequest = new ScanRequest(TEntity.TableName) { ProjectionExpression = "pk, sk" };

        ScanResponse response = await _dynamoDb.ScanAsync(scanRequest);

        return response.Items.Select(item => (item["pk"].S, item["sk"].S)).ToList();
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
        Dictionary<string, AttributeValue> keyAttributeValues = getPkSkAttributeValues(partitionKey, sortKey);

        var deletedItemRequest = new DeleteItemRequest(TEntity.TableName, keyAttributeValues);

        DeleteItemResponse response = await _dynamoDb.DeleteItemAsync(deletedItemRequest);

        // Without a ConditionExpression, the response is OK, even if there was no entity to delete
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<int> CountItems(string partitionKey)
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_Pk", new AttributeValue(partitionKey) }
        };

        var queryRequest = new QueryRequest
        {
            TableName                 = TEntity.TableName,
            KeyConditionExpression    = "pk = :v_Pk",
            ExpressionAttributeValues = expressionAttributeValues,
            Select                    = Select.COUNT, // No attributes need to be returned to minimize resource usage
            Limit                     = 1_000         // Set a limit for pagination
        };

        int totalCount = 0;

        do
        {
            QueryResponse queryResponse = await _dynamoDb.QueryAsync(queryRequest);

            totalCount += queryResponse.Count ?? 0;

            queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;

        } while (queryRequest.ExclusiveStartKey?.Count > 0);

        return totalCount;
    }

    protected static Dictionary<string, AttributeValue> getPkSkAttributeValues(Guid partitionKey, Guid sortKey)
    {
        return getPkSkAttributeValues(partitionKey.ToString(), sortKey.ToString());
    }

    protected static Dictionary<string, AttributeValue> getPkSkAttributeValues(string partitionKey, string sortKey)
    {
        var keyAttributeValues = new Dictionary<string, AttributeValue>
        {
            { "pk", new AttributeValue(partitionKey) },
            { "sk", new AttributeValue(sortKey) }
        };

        return keyAttributeValues;
    }

    protected static Dictionary<string, AttributeValue> entityToAttributeValues(TEntity entity)
    {
        string serializedEntity = JsonSerializer.Serialize(entity);

        return Document.FromJson(serializedEntity).ToAttributeMap();
    }

    protected static TEntity? attributeValuesToEntity(Dictionary<string, AttributeValue>? attributeValues)
    {
        if (attributeValues is null || attributeValues.Count == 0)
        {
            return default;
        }

        Document document = Document.FromAttributeMap(attributeValues);

        return JsonSerializer.Deserialize<TEntity>(document.ToJson());
    }

    private static Dictionary<string, AttributeValue>? continuationTokenToExclusiveStartKey(string? continuationToken)
    {
        return string.IsNullOrEmpty(continuationToken)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(Convert.FromBase64String(continuationToken));
    }

    private static string? lastEvaluatedKeyToContinuationToken(Dictionary<string, AttributeValue>? lastEvaluatedKey)
    {
        return lastEvaluatedKey is null
            ? null
            : Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(lastEvaluatedKey));
    }
}
