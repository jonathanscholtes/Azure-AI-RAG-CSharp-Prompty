using ChatAPI.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace ChatAPI.Services;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public sealed class ChatService(Kernel kernel, ITextEmbeddingGenerationService embedding, CustomerData customerData, ChatHistoryData chatHistoryData, AISearchData aiSearch, ILogger<ChatService> logger)
{
    private readonly CustomerData _customerData = customerData;
    private readonly AISearchData _aiSearch = aiSearch;
    private readonly ILogger<ChatService> _logger = logger;

    private readonly ChatHistoryData _chatHistoryData = chatHistoryData;

    private readonly Kernel _kernel = kernel;
    private readonly ITextEmbeddingGenerationService _embedding = embedding;
    private readonly KernelFunction _chat = kernel.CreateFunctionFromPromptyFile("chat.prompty");

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
       
        _logger.LogInformation("Getting result.");
        _logger.LogInformation("Customer: {customer}", customer);

        string? answer = await _chat.InvokeAsync<string>(_kernel, new()
        {
            { "customer", customer },
            { "documentation", context },
            { "question", question },
        });


        _logger.LogInformation("Answer: {Answer}", answer);

        // Save the chat interaction to Cosmos DB
        await _chatHistoryData.SaveChatAsync(customerId, question, answer);

        _logger.LogInformation("Chat interaction saved for customerId: {CustomerId}", customerId);


        return JsonSerializer.Serialize(new { answer, context });
    }


}