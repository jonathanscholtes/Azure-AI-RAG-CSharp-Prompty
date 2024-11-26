using System.Text;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using System.Text.Json.Serialization;

namespace ChatAPI.Data;



public class Customer
{
    public string id { get; set; } = default!;
    public string firstname { get; set; } = default!;
     public string lastname { get; set; } = default!;
    public string email { get; set; } = default!;
    public string phonenumber { get; set; } = default!;
}

[JsonSerializable(typeof(List<Customer>))]
[JsonSerializable(typeof(Customer))]
public partial class CustomerJsonContext : JsonSerializerContext
{
}



public sealed class GenerateCustomerInfo(ILogger<GenerateCustomerInfo> logger, IConfiguration config, CosmosClient cosmosClient)
{
    private readonly ILogger<GenerateCustomerInfo> _logger = logger;
    private readonly CosmosClient _cosmosClient = cosmosClient;
    private readonly string _databaseName = config["CosmosDb:databaseName"]!;
    private readonly string _containerName = config["CosmosDb:containerName"]!;

    public async Task PopulateCosmosAsync()
    {
        try
        {
            var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await database.Database.CreateContainerIfNotExistsAsync(_containerName, "/id");

            var numDocs = 0;

            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            using (var iterator = container.Container.GetItemQueryIterator<int>(query))
            {
                var result = await iterator.ReadNextAsync();
                numDocs = result.FirstOrDefault();
            }

            if (numDocs == 0)
            {
                _logger.LogInformation("Creating CosmosDB container {ContainerName} in database {DatabaseName}...", _containerName, _databaseName);

            
                var filePath = "./Data/sample_data/customers/customers.json"; // Path to the single JSON file

                // Read the content of the JSON file
                var content = await File.ReadAllTextAsync(filePath);

                // Deserialize the JSON array using the source generator context
                var customers = JsonSerializer.Deserialize(content, CustomerJsonContext.Default.ListCustomer)!;

                // Iterate through each customer in the array and upsert them
                foreach (var customer in customers)
                {
                    // Serialize each customer back to a JSON string
                    var customerContent = JsonSerializer.Serialize(customer, CustomerJsonContext.Default.Customer);

                    _logger.LogInformation("Customer Content: " + customerContent);

                    // Create a memory stream from the serialized JSON
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(customerContent));

                    // Upsert the item into the container
                    await container.Container.CreateItemStreamAsync(stream, new PartitionKey(customer.id));

                    // Log the success message
                    _logger.LogInformation("Upserted item with id {CustomerID}", customer.id);
                }
            }
            else
            {
                _logger.LogInformation("CosmosDB container already populated, nothing to do.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating Cosmos");
        }
    }
}