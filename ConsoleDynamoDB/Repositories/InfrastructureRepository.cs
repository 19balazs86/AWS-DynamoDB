using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public sealed class InfrastructureRepository(IAmazonDynamoDB _dynamoDb)
{
    public static string GetLocalSecondaryIndexName<TEntity>() where TEntity : IEntity
    {
        return $"{TEntity.TableName}_pk_lsi";
    }

    public async Task<bool> IsTableExists<TEntity>() where TEntity : IEntity
    {
        ListTablesResponse listTablesResponse = await _dynamoDb.ListTablesAsync();

        return listTablesResponse.TableNames.Contains(TEntity.TableName);
    }

    public async Task EnsureTableCreated<TEntity>() where TEntity : IEntity
    {
        if (await IsTableExists<TEntity>())
        {
            return;
        }

        await createTable<TEntity>();
    }

    private async Task createTable<TEntity>() where TEntity : IEntity
    {
        List<AttributeDefinition> attributeDefinitions =
        [
            new("pk", ScalarAttributeType.S),
            new("sk", ScalarAttributeType.S),

            // 'pk' and 'sk' would be sufficient, but 'lsi' is used in the LocalSecondaryIndex, so it also needs to be included
            new("lsi", ScalarAttributeType.S)
        ];

        List<KeySchemaElement> keySchemaElements = [new("pk", KeyType.HASH), new("sk", KeyType.RANGE)];

        var throughput = new ProvisionedThroughput { ReadCapacityUnits = 1, WriteCapacityUnits = 1 };

        string tableName = TEntity.TableName;

        var createTableRequest = new CreateTableRequest
        {
            TableName             = tableName,
            AttributeDefinitions  = attributeDefinitions,
            KeySchema             = keySchemaElements,
            ProvisionedThroughput = throughput,
            BillingMode           = BillingMode.PROVISIONED,
            LocalSecondaryIndexes = getLocalSecondaryIndexes<TEntity>()
        };

        CreateTableResponse createTableResponse = await _dynamoDb.CreateTableAsync(createTableRequest);

        Console.WriteLine($"Created table: {tableName} | StatusCode: {createTableResponse.HttpStatusCode}");
    }

    private static List<LocalSecondaryIndex> getLocalSecondaryIndexes<TEntity>() where TEntity : IEntity
    {
        List<KeySchemaElement> keySchemaElements = [new("pk", KeyType.HASH), new("lsi", KeyType.RANGE)];

        var localSecondaryIndex = new LocalSecondaryIndex
        {
            IndexName  = GetLocalSecondaryIndexName<TEntity>(),
            KeySchema  = keySchemaElements,
            Projection = new Projection { ProjectionType = ProjectionType.ALL }
        };

        return [localSecondaryIndex];
    }
}
