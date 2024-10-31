using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public sealed class InfrastructureRepository(IAmazonDynamoDB _dynamoDb)
{
    public async Task EnsureTableExists<TEntity>() where TEntity : IEntity
    {
        ListTablesResponse listTablesResponse = await _dynamoDb.ListTablesAsync();

        string tableName = TEntity.TableName;

        if (listTablesResponse.TableNames.Contains(tableName))
        {
            return;
        }

        await createTable(tableName);
    }

    private async Task createTable(string tableName)
    {
        List<AttributeDefinition> attributeDefinitions = [new("pk", ScalarAttributeType.S), new("sk", ScalarAttributeType.S)];
        List<KeySchemaElement>    keySchemaElements    = [new("pk", KeyType.HASH),          new("sk", KeyType.RANGE)];

        var throughput = new ProvisionedThroughput { ReadCapacityUnits = 1, WriteCapacityUnits = 1 };

        var createTableRequest = new CreateTableRequest
        {
            TableName             = tableName,
            AttributeDefinitions  = attributeDefinitions,
            KeySchema             = keySchemaElements,
            ProvisionedThroughput = throughput,
            BillingMode           = BillingMode.PROVISIONED
        };

        CreateTableResponse createTableResponse = await _dynamoDb.CreateTableAsync(createTableRequest);

        Console.WriteLine($"Created table: {tableName} | StatusCode: {createTableResponse.HttpStatusCode}");
    }
}
