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

        List<(Guid TenantId,   Guid[] UserIds)>                  users     = await createUsers(dynamoDb, tenantIds);
        List<(Guid TenantId,   Guid UserId, Guid[] BlogPostIds)> blogPosts = await createBlogPosts(dynamoDb, users);
        List<(Guid BlogPostId, Guid UserId, Guid[] CommentIds)>  comments  = await createComments(dynamoDb, tenantIds);

        await updateBlogPostsByUserIds(dynamoDb, users);

        List<(Guid UserId, BlogPost[] BlogPosts)> userBlogPosts = await getBlogPostsUsingIndex(dynamoDb, users[0].TenantId, users.SelectMany(x => x.UserIds).ToArray());

        Guid[] randomBlogPostIds = Random.Shared.GetItems(blogPosts[0].BlogPostIds, 5);

        List<(Guid BlogPostId, Rating Rating)> blogPostRatings = await getBlogPostRatings(dynamoDb, blogPosts[0].TenantId, randomBlogPostIds);

        Rating? rating = await starRating(dynamoDb, blogPosts[0].TenantId, blogPosts[0].BlogPostIds[0]);

        int blogPostCount = await getBlogPostsCount(dynamoDb, tenantIds[0]);
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

                await userRepository.AddItem(user);
            }
        }

        return users;
    }

    private static async Task<List<(Guid TenantId, Guid UserId, Guid[] BlogPostIds)>> createBlogPosts(IAmazonDynamoDB dynamoDb, List<(Guid TenantId, Guid[] UserIds)> users)
    {
        List<(Guid TenantId, Guid UserId, Guid[] BlogPostIds)> blogPosts = [];

        var blogPostRepository = new GenericRepository<BlogPost>(dynamoDb);

        foreach ((Guid tenantId, Guid[] userIds) in users)
        {
            foreach (Guid userId in userIds)
            {
                Guid[] blogPostIds = [Guid.NewGuid(), Guid.NewGuid()];

                blogPosts.Add((tenantId, userId, blogPostIds));

                foreach (Guid blogPostId in blogPostIds)
                {
                    var blogPost = new BlogPost
                    {
                        Id       = blogPostId,
                        TenantId = tenantId,
                        UserId   = userId,
                        Title    = $"Title #{blogPostId.ToString()[..5]}",
                        Content  = $"Content #{blogPostId.ToString()[..5]}",
                        Rating   = new Rating { Avg = 4.5, Count = 2, Sum = 9 }
                    };

                    await blogPostRepository.AddItem(blogPost);
                }
            }
        }

        return blogPosts;
    }

    private static async Task<List<(Guid BlogPostId, Guid UserId, Guid[] CommentIds)>> createComments(IAmazonDynamoDB dynamoDb, Guid[] tenantIds)
    {
        List<(Guid BlogPostId, Guid UserId, Guid[] CommentIds)> comments = [];

        var blogPostRepository = new GenericRepository<BlogPost>(dynamoDb);
        var commentRepository  = new GenericRepository<Comment>(dynamoDb);

        foreach (Guid tenantId in tenantIds)
        {
            BlogPost[] blogPosts = await blogPostRepository.GetItemsByPartition(tenantId.ToString());

            foreach (BlogPost blogPost in blogPosts)
            {
                Guid[] commentIds = [Guid.NewGuid(), Guid.NewGuid()];

                comments.Add((blogPost.Id, blogPost.UserId, commentIds));

                foreach (Guid commentId in commentIds)
                {
                    var comment = new Comment
                    {
                        Id         = commentId,
                        UserId     = blogPost.UserId,
                        BlogPostId = blogPost.Id,
                        Text       = $"Content #{commentId.ToString()[..5]}"
                    };

                    await commentRepository.AddItem(comment);
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

    private static async Task<List<(Guid UserId, BlogPost[] BlogPosts)>> getBlogPostsUsingIndex(IAmazonDynamoDB dynamoDb, Guid tenantId, Guid[] userIds)
    {
        var blogPostRepository = new GenericRepository<BlogPost>(dynamoDb);

        List<(Guid UserId, BlogPost[] BlogPosts)> userBlogPosts = [];

        foreach (Guid userId in userIds)
        {
            BlogPost[] blogPosts = await blogPostRepository.GetItemsUsingIndex(tenantId.ToString(), userId.ToString());

            userBlogPosts.Add((userId, blogPosts));
        }

        return userBlogPosts;
    }

    private static async Task<List<(Guid BlogPostId, Rating Rating)>> getBlogPostRatings(IAmazonDynamoDB dynamoDb, Guid tenantId, Guid[] blogPostIds)
    {
        var blogPostRepository = new BlogPostRepository(dynamoDb);

        List<(Guid BlogPostId, Rating Rating)> blogPosts = [];

        foreach (Guid blogPostId in blogPostIds)
        {
            Rating? rating = await blogPostRepository.GetRating(tenantId, blogPostId);

            if (rating is not null)
            {
                blogPosts.Add((blogPostId, rating));
            }
        }

        return blogPosts;
    }

    private static async Task<Rating?> starRating(IAmazonDynamoDB dynamoDb, Guid tenantId, Guid blogPostId)
    {
        var blogPostRepository = new BlogPostRepository(dynamoDb);

        return await blogPostRepository.AddRating(tenantId, blogPostId, Random.Shared.Next(1, 6));
    }

    private static async Task<int> getBlogPostsCount(IAmazonDynamoDB dynamoDb, Guid tenantId)
    {
        var blogPostRepository = new GenericRepository<BlogPost>(dynamoDb);

        return await blogPostRepository.CountItems(tenantId.ToString());
    }

    private static async Task ensureTablesExists(IAmazonDynamoDB dynamoDb)
    {
        var repository = new InfrastructureRepository(dynamoDb);

        await repository.EnsureTableCreated<User>();
        await repository.EnsureTableCreated<BlogPost>();
        await repository.EnsureTableCreated<Comment>();
    }
}
