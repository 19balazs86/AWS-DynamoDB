using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public interface IBlogPostRepository : IGenericRepository<BlogPost>
{
    Task<Rating?> GetRatingByProjection(Guid tenantId, Guid blogPostId);
}

public sealed class BlogPostRepository(IAmazonDynamoDB _dynamoDb) : GenericRepository<BlogPost>(_dynamoDb), IBlogPostRepository
{
    public async Task<Rating?> GetRatingByProjection(Guid tenantId, Guid blogPostId)
    {
        // AmazonDynamoDBException: Invalid ProjectionExpression: Attribute names are reserved keywords: Sum, Count, Avg
        // ProjectionExpression = "Rating.Sum, Rating.Count, Rating.Avg" -- I can not use it
        var expressionAttributeNames = new Dictionary<string, string>
        {
            ["#sum"]   = "Sum",
            ["#count"] = "Count",
            ["#avg"]   = "Avg"
        };

        var keyAttributeValues = new Dictionary<string, AttributeValue>
        {
            { "pk", new AttributeValue(tenantId.ToString()) },
            { "sk", new AttributeValue(blogPostId.ToString()) }
        };

        var getItemRequest = new GetItemRequest(BlogPost.TableName, keyAttributeValues)
        {
            ProjectionExpression     = "Rating.#sum, Rating.#count, Rating.#avg",
            ExpressionAttributeNames = expressionAttributeNames
        };

        GetItemResponse response = await _dynamoDb.GetItemAsync(getItemRequest);

        if (response.Item is null || response.Item.Count == 0)
        {
            return default;
        }

        Document document = Document.FromAttributeMap(response.Item);

        document = document["Rating"].AsDocument();

        return new Rating
        {
            Avg   = document["Avg"].AsDouble(),
            Count = document["Count"].AsInt(),
            Sum   = document["Sum"].AsInt()
        };
    }
}
