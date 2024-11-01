using Amazon.DynamoDBv2;
using ConsoleDynamoDB.Entities;
using ConsoleDynamoDB.Repositories;

namespace ConsoleDynamoDB;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // You would use it like: services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        using var dynamoDb = new AmazonDynamoDBClient();

        await ensureTablesExists(dynamoDb);

        Guid[] tenantIds = [Guid.NewGuid(), Guid.NewGuid()];

        List<(Guid TenantId,   Guid[] UserIds)>          users     = await createUsers(dynamoDb, tenantIds);
        List<(Guid TenantId,   string UserId_PostId)>    blogPosts = await createBlogPosts(dynamoDb, users);
        List<(Guid BlogPostId, string UserId_CommentId)> comments  = await createComments(dynamoDb, tenantIds);

        await updateBlogPostsByUserIds(dynamoDb, users);
    }

    private static async Task<List<(Guid TenantId, Guid[] UserIds)>> createUsers(IAmazonDynamoDB dynamoDb, Guid[] tenantIds)
    {
        List<(Guid TenantId, Guid[] UserIds)> users = [];

        var userRepository = new GenericRepository<User>(dynamoDb);

        foreach (Guid tenantId in tenantIds)
        {
            Guid[] usersIds = [Guid.NewGuid(), Guid.NewGuid()];

            users.Add((tenantId, usersIds));

            foreach (Guid usersId in usersIds)
            {
                var user = new User
                {
                    Id       = usersId,
                    TenantId = tenantId,
                    Name     = $"John Doe #{usersId.ToString()[..5]}"
                };

                await userRepository.CreateItem(user);
            }
        }

        return users;
    }

    private static async Task<List<(Guid TenantId, string UserId_PostId)>> createBlogPosts(IAmazonDynamoDB dynamoDb, List<(Guid TenantId, Guid[] UserIds)> users)
    {
        List<(Guid TenantId, string UserId_PostId)> blogPosts = [];

        var blogPostRepository = new GenericRepository<BlogPost>(dynamoDb);

        foreach ((Guid tenantId, Guid[] userIds) in users)
        {
            foreach (Guid userId in userIds)
            {
                Guid[] blogPostIds = [Guid.NewGuid(), Guid.NewGuid()];

                foreach (Guid blogPostId in blogPostIds)
                {
                    var blogPost = new BlogPost
                    {
                        Id       = blogPostId,
                        TenantId = tenantId,
                        UserId   = userId,
                        Title    = $"Title #{blogPostId.ToString()[..5]}",
                        Content  = $"Content #{blogPostId.ToString()[..5]}"
                    };

                    await blogPostRepository.CreateItem(blogPost);

                    blogPosts.Add((tenantId, blogPost.Sk));
                }
            }
        }

        return blogPosts;
    }

    private static async Task<List<(Guid BlogPostId, string UserId_CommentId)>> createComments(IAmazonDynamoDB dynamoDb, Guid[] tenantIds)
    {
        List<(Guid BlogPostId, string UserId_CommentId)> comments = [];

        var blogPostRepository = new GenericRepository<BlogPost>(dynamoDb);
        var commentRepository  = new GenericRepository<Comment>(dynamoDb);

        foreach (Guid tenantId in tenantIds)
        {
            BlogPost[] blogPosts = await blogPostRepository.GetItems(tenantId.ToString());

            foreach (BlogPost blogPost in blogPosts)
            {
                Guid[] commentIds = [Guid.NewGuid(), Guid.NewGuid()];

                foreach (Guid commentId in commentIds)
                {
                    var comment = new Comment
                    {
                        Id         = commentId,
                        UserId     = blogPost.UserId,
                        BlogPostId = blogPost.Id,
                        Text       = $"Content #{commentId.ToString()[..5]}"
                    };

                    await commentRepository.CreateItem(comment);

                    comments.Add((blogPost.Id, comment.Sk));
                }
            }
        }

        return comments;
    }

    private static async Task updateBlogPostsByUserIds(IAmazonDynamoDB dynamoDb, List<(Guid TenantId, Guid[] UserIds)> users)
    {
        var blogPostRepository = new GenericRepository<BlogPost>(dynamoDb);

        foreach ((Guid tenantId, Guid[] userIds) in users)
        {
            foreach (Guid userId in userIds)
            {
                BlogPost[] blogPosts = await blogPostRepository.GetItemsBySortKeyPrefix(tenantId.ToString(), userId.ToString());

                foreach (BlogPost blogPost in blogPosts)
                {
                    blogPost.Title   = $"Updated - {blogPost.Title}";
                    blogPost.Content = $"Updated - {blogPost.Content}";

                    await blogPostRepository.UpdateItem(blogPost);
                }
            }
        }
    }

    private static async Task ensureTablesExists(IAmazonDynamoDB dynamoDb)
    {
        var repository = new InfrastructureRepository(dynamoDb);

        await repository.EnsureTableExists<User>();
        await repository.EnsureTableExists<BlogPost>();
        await repository.EnsureTableExists<Comment>();
    }
}
