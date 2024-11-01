using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ConsoleDynamoDB.Entities;

namespace ConsoleDynamoDB.Repositories;

public interface IBlogPostRepository : IGenericRepository<BlogPost>
{
    Task<BlogPost?> GetBlogPost(Guid tenantId, Guid blogPostId);
}

public sealed class BlogPostRepository(IAmazonDynamoDB _dynamoDb) : GenericRepository<BlogPost>(_dynamoDb), IBlogPostRepository
{
    public async Task<BlogPost?> GetBlogPost(Guid tenantId, Guid blogPostId)
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_Pk",         new AttributeValue(tenantId.ToString())   },
            { ":v_BlogPostId", new AttributeValue(blogPostId.ToString()) }
        };

        var queryRequest = new QueryRequest
        {
            TableName                 = BlogPost.TableName,
            IndexName                 = "BlogPosts_pk_Id",
            KeyConditionExpression    = "pk = :v_Pk and Id = :v_BlogPostId",
            ExpressionAttributeValues = expressionAttributeValues
        };

        QueryResponse response = await _dynamoDb.QueryAsync(queryRequest);

        return response.Items.Count == 0 ? null : attributeValueToEntity(response.Items.Single());
    }
}
