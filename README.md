# Playing with AWS DynamoDB

- This repository contains a console application that works with DynamoDB
- The business domain is a straightforward, multi-tenant blogging system
- I implemented a [GenericRepository](ConsoleDynamoDB/Repositories/GenericRepository.cs) for CRUD operations, which can be extended to meet specific business needs for each entity

## DynamoDBContext vs IAmazonDynamoDB

- In the examples, I used `IAmazonDynamoDB` as a low-level interface, which provides direct access to all DynamoDB operations
- Since it is a low-level interface, it typically involves more boilerplate code
- On the other hand, `DynamoDBContext` is a higher-level interface that provides an abstraction over the IAmazonDynamoDB service
- While it simplifies many tasks, it also reduces the level of control and granularity you have over the underlying requests

## Resources

#### 🧰 `AWS .NET SDK`

- [Documentation](https://docs.aws.amazon.com/sdk-for-net)
- [Developer Guide](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/welcome.html)
- [API Reference](https://docs.aws.amazon.com/sdkfornet/v3/apidocs)

#### ☁️ `DynamoDB`

- [API Reference](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/Welcome.html) | [Developer Guide](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html)
- [CloudFormation templates](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/template-guide.html) | [DynamoDB Table](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-dynamodb-table.html)

#### 📓 `Blog posts from Rahul Nath`

- [Querying in DynamoDB](https://www.rahulpnath.com/blog/dynamodb-querying-dotnet) | [Video](https://youtu.be/iv6OKueqBd4) 📽️*37 min*
- [UpdateItem vs. PutItem](https://www.rahulpnath.com/blog/dynamodb-putitem-vs-updateitem-whats-the-difference) | [Video](https://youtu.be/VXNEaYZ1EXs) 📽️*19 min*
- [Implementing optimistic locking consistency](https://www.rahulpnath.com/blog/dynamodb-optimistic-locking) | [Video](https://youtu.be/k5eMcb0-nsI) 📽️*21 min*
- [Transactions for complex operations](https://www.rahulpnath.com/blog/amazon-dynamodb-transactions-dotnet) | [Video](https://youtu.be/kQ14L2pEZic) 📽️*23 min*
- [Pagination](https://www.rahulpnath.com/blog/dynamodb-pagination) | [Video](https://youtu.be/IXz04U73MxA) 📽️*40 min*