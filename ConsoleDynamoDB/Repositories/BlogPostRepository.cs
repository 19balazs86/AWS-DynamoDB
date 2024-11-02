using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public interface IBlogPostRepository : IGenericRepository<BlogPost>
{
    Task<Rating?> GetRating(Guid tenantId, Guid blogPostId);

    Task<Rating?> AddRating(Guid tenantId, Guid blogPostId, int rating);
}

public sealed class BlogPostRepository(IAmazonDynamoDB _dynamoDb) : GenericRepository<BlogPost>(_dynamoDb), IBlogPostRepository
{
    // This method uses projection to return only a portion of the document

    public async Task<Rating?> GetRating(Guid tenantId, Guid blogPostId)
    {
        Dictionary<string, AttributeValue> keyAttributeValues = getPkSkAttributeValues(tenantId, blogPostId);

        var getItemRequest = new GetItemRequest(BlogPost.TableName, keyAttributeValues)
        {
            ProjectionExpression     = "Rating.#sum, Rating.#count, Rating.#avg",
            ExpressionAttributeNames = _ratingExpressionAttributeNames
        };

        GetItemResponse response = await _dynamoDb.GetItemAsync(getItemRequest);

        if (response.Item is null || response.Item.Count == 0)
        {
            return null;
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

    // This method uses an UpdateExpression to update only part of the document

    public async Task<Rating?> AddRating(Guid tenantId, Guid blogPostId, int rating)
    {
        Rating? oldRating = await GetRating(tenantId, blogPostId);

        if (oldRating is null)
        {
            return null;
        }

        Rating newRating = oldRating.CreateNewWith(rating);

        Dictionary<string, AttributeValue> keyAttributeValues = getPkSkAttributeValues(tenantId, blogPostId);

        var culture = new CultureInfo("en-US");

        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            [":v_Count"]    = new() { N = newRating.Count.ToString() },
            [":v_Sum"]      = new() { N = newRating.Sum.ToString() },
            [":v_Avg"]      = new() { N = newRating.Avg.ToString(culture) },
            [":v_OldCount"] = new() { N = oldRating.Count.ToString() }
        };

        var updateItemRequest = new UpdateItemRequest
        {
            TableName                 = BlogPost.TableName,
            Key                       = keyAttributeValues,
            UpdateExpression          = "SET Rating.#count = :v_Count, Rating.#sum = :v_Sum, Rating.#avg = :v_Avg",
            ExpressionAttributeNames  = _ratingExpressionAttributeNames,
            ExpressionAttributeValues = expressionAttributeValues,
            ConditionExpression       = "Rating.#count = :v_OldCount", // Ensuring there are no updates in the meantime
            ReturnValues              = ReturnValue.UPDATED_NEW
        };

        UpdateItemResponse updateItemResponse = await _dynamoDb.UpdateItemAsync(updateItemRequest);

        Document document = Document.FromAttributeMap(updateItemResponse.Attributes);

        string json = document.ToJson();

        return newRating;
    }

    // I can not use this ProjectionExpression = "Rating.Sum, Rating.Count, Rating.Avg"
    // AmazonDynamoDBException: Invalid ProjectionExpression: Attribute names are reserved keywords: Sum, Count, Avg
    // https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ReservedWords.html
    private static readonly Dictionary<string, string> _ratingExpressionAttributeNames = new()
    {
        ["#sum"]   = nameof(Rating.Sum),
        ["#count"] = nameof(Rating.Count),
        ["#avg"]   = nameof(Rating.Avg)
    };
}
