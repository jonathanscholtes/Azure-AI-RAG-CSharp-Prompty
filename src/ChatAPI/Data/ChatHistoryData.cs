using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ChatAPI.Data;

public sealed class ChatHistoryData(CosmosClient cosmosClient, ILogger<CustomerData> logger, IConfiguration config)
{
    // Assume a Cosmos DB client is injected for operations
     private readonly CosmosClient _cosmosClient = cosmosClient;
    private readonly ILogger<CustomerData> logger = logger;
    private readonly string _databaseName = config["CosmosDb_Database"]!;

    private readonly string _containerName = config["CosmosDb_ChatContainer"]!;
   

    public async Task SaveChatAsync(string customerId, string question, string? answer)
    {
        // Save the question-answer pair into Cosmos DB with customerId and timestamp
        //var chatRecord = new {SessionId=Guid.NewGuid().ToString(), customerId, question, answer, timestamp = DateTime.UtcNow };

        var chatRecord = new
        {
            id = Guid.NewGuid().ToString(), 
            sessionId = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            Question = question,
            Answer = answer,
            Timestamp = DateTime.UtcNow
        };

        var container = _cosmosClient.GetContainer(_databaseName, _containerName);
        //await _cosmosClient.SaveAsync(chatRecord);  // Implement SaveAsync based on your Cosmos DB setup

        PartitionKey partitionKey = new PartitionKey(chatRecord.sessionId);
        await container.CreateItemAsync(
            item: chatRecord,
            partitionKey: partitionKey
        );
    }
}
