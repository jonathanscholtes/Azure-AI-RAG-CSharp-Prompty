using ChatAPI.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace ChatAPI.Services;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public sealed class ChatService(Kernel kernel, ITextEmbeddingGenerationService embedding, CustomerData customerData, AISearchData aiSearch, ILogger<ChatService> logger)
{
    private readonly CustomerData _customerData = customerData;
    private readonly AISearchData _aiSearch = aiSearch;
    private readonly ILogger<ChatService> _logger = logger;

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

        var context = await _aiSearch.RetrieveDocumentationAsync(question, embedding);

        _logger.LogInformation("Getting result.");
        string? answer = await _chat.InvokeAsync<string>(_kernel, new()
        {
            { "customer", customer },
            { "documentation", context },
            { "question", question },
        });


        _logger.LogInformation("Answer: {Answer}", answer);

        return JsonSerializer.Serialize(new { answer, context });
    }


}