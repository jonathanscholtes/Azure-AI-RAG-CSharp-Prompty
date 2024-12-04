using ChatAPI.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace ChatAPI.Services;


public sealed class ChatService(Kernel kernel, ITextEmbeddingGenerationService embedding, CustomerData customerData, ChatHistoryData chatHistoryData, AISearchData aiSearch, ILogger<ChatService> logger)
{
    private readonly CustomerData _customerData = customerData;
    private readonly AISearchData _aiSearch = aiSearch;
    private readonly ILogger<ChatService> _logger = logger;

    private readonly ChatHistoryData _chatHistoryData = chatHistoryData;

    private readonly Kernel _kernel = kernel;
    private readonly ITextEmbeddingGenerationService _embedding = embedding;
    //private readonly KernelFunction _chat = kernel.CreateFunctionFromPromptyFile("chat.prompty");

    private readonly KernelFunction _chat = kernel.CreateFunctionFromPromptYaml(File.ReadAllText("chat.yaml"), promptTemplateFactory: new HandlebarsPromptTemplateFactory());

    public async Task<string> GetResponseAsync(string customerId, string question)
    {
        _logger.LogInformation("CustomerId = {CustomerID}, Question = {Question}", customerId, question);

        var customerTask = _customerData.GetCustomerAsync(customerId);
        var embeddingTask = _embedding.GenerateEmbeddingAsync(question);
        await Task.WhenAll(customerTask, embeddingTask);
        var customer = await customerTask;
        var embedding = await embeddingTask;


        // Log embedding in a readable format
        string embeddingString = JsonSerializer.Serialize(embedding);
        _logger.LogInformation("Embedding: {Embedding}", embeddingString);
        
        var context = await _aiSearch.RetrieveDocumentationAsync(question, embedding);
        var imageBase64List = await GetImagesAsBase64Async(["https://sachatapidemo.blob.core.windows.net/images/temp_diagram.png"]);

        
        _logger.LogInformation("Getting result.");
        _logger.LogInformation("Customer: {customer}", customer);

        //string customerJson = JsonSerializer.Serialize(customer);
        //string documentationJson = JsonSerializer.Serialize(context);
       
        //_logger.LogInformation("Customer JSON: {CustomerJson}", customerJson);
        //_logger.LogInformation("Documentation JSON: {DocumentationJson}", documentationJson);

        _logger.LogInformation("images JSON: {images}", JsonSerializer.Serialize(imageBase64List));

        string? answer = await _chat.InvokeAsync<string>(_kernel, new()
        {
            { "customer", customer },
            { "documentation", context },
            { "question", question },
            { "images", JsonSerializer.Serialize(imageBase64List) }
        });


        _logger.LogInformation("Answer: {Answer}", answer);

        // Save the chat interaction to Cosmos DB
        await _chatHistoryData.SaveChatAsync(customerId, question, answer);

        _logger.LogInformation("Chat interaction saved for customerId: {CustomerId}", customerId);


        return JsonSerializer.Serialize(new { answer, context });
    }

    private async Task<List<string>> GetImagesAsBase64Async(List<string> imageBlobUrls)
    {
        var tasks = imageBlobUrls.Select(async url =>
        {
            var blobClient = new BlobClient(new Uri(url),new DefaultAzureCredential());
            using var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
        });

        var base64Images = await Task.WhenAll(tasks);
        return base64Images.ToList();
    }
}