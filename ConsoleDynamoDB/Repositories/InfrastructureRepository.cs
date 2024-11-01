using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public sealed class InfrastructureRepository(IAmazonDynamoDB _dynamoDb)
{
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

            // ID is used in the LocalSecondaryIndex, so it needs to be included, otherwise, the pk and sk would suffice
            new(nameof(IEntity.Id), ScalarAttributeType.S)
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
        // The BlogPost and Comment entities have a composite sort key that combines the UserId and the entity's own ID
        // Adding a LocalSecondaryIndex enables queries using by pk and ID
        List<KeySchemaElement> keySchemaElements = [new("pk", KeyType.HASH), new(nameof(IEntity.Id), KeyType.RANGE)];

        var localSecondaryIndex = new LocalSecondaryIndex
        {
            IndexName  = $"{TEntity.TableName}_pk_Id",
            KeySchema  = keySchemaElements,
            Projection = new Projection { ProjectionType = ProjectionType.ALL }
        };

        return [localSecondaryIndex];
    }
}
