# Playing with AWS DynamoDB

- This repository contains a console application that works with DynamoDB
- The business domain is a straightforward, multi-tenant blogging system
- I implemented a [GenericRepository](ConsoleDynamoDB/Repositories/GenericRepository.cs) for CRUD operations, which can be extended to meet specific business needs for each entity

## Resources

- [CloudFormation templates](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/template-guide.html): [DynamoDB Table](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-dynamodb-table.html)

#### 📓 `Blog posts from Rahul Nath`

- [UpdateItem vs. PutItem](https://youtu.be/VXNEaYZ1EXs) | [Video](https://youtu.be/VXNEaYZ1EXs) 📽️*19 min*
- [Implementing optimistic locking consistency](https://www.rahulpnath.com/blog/dynamodb-optimistic-locking) | [Video](https://youtu.be/k5eMcb0-nsI) 📽️*21 min*
- [Transactions for complex operations](https://www.rahulpnath.com/blog/amazon-dynamodb-transactions-dotnet) | [Video](https://youtu.be/kQ14L2pEZic) 📽️*23 min*
- [Pagination](https://www.rahulpnath.com/blog/dynamodb-pagination) | [Video](https://youtu.be/IXz04U73MxA) 📽️*40 min*