using Amazon.DynamoDBv2;
using ConsoleDynamoDB.Entities;
using ConsoleDynamoDB.Repositories;

namespace ConsoleDynamoDB;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // You would use it like: services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        using var dynamoDbClient = new AmazonDynamoDBClient();

        await ensureTablesExists(dynamoDbClient);
    }

    private static async Task ensureTablesExists(AmazonDynamoDBClient dynamoDbClient)
    {
        var repository = new InfrastructureRepository(dynamoDbClient);

        await repository.EnsureTableExists<User>();
        await repository.EnsureTableExists<BlogPost>();
        await repository.EnsureTableExists<Comment>();
    }
}
