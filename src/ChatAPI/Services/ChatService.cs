using ChatAPI.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Identity;
using ChatAPI.Plugins;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ChatAPI.Services;



public sealed class ChatService(Kernel kernel, ITextEmbeddingGenerationService embedding, CustomerData customerData, AISearchData aiSearch, ILogger<ChatService> logger)
{
    private readonly CustomerData _customerData = customerData;
    private readonly AISearchData _aiSearch = aiSearch;
    private readonly ILogger<ChatService> _logger = logger;

    //private readonly ChatHistoryData _chatHistoryData = chatHistoryData;

    private readonly Kernel _kernel = kernel;
    private readonly ITextEmbeddingGenerationService _embedding = embedding;
    //private readonly KernelFunction _chat = kernel.CreateFunctionFromPromptyFile("chat.prompty");

    private readonly KernelFunction _chat = kernel.CreateFunctionFromPromptYaml(File.ReadAllText("chat.yaml"), promptTemplateFactory: new HandlebarsPromptTemplateFactory());

    

    public async Task<string> GetResponseAsync(string customerId, string question)
    {


        ChatHistory chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a Technical Support Assistant for Cloud Services and Computer Solutions. Your role is to provide brief, clear, and friendly responses to customers' cloud and computer troubleshooting questions and can analyze images. You strive to be helpful, personable, and even add a touch of personality when appropriate—like including emojis. Always include the customer's name in your responses for a personalized touch. Analyze and describe the images.");
        chatHistory.AddUserMessage(question);

        IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        PromptExecutionSettings settings = new() { FunctionChoiceBehavior = Microsoft.SemanticKernel.FunctionChoiceBehavior.Auto(autoInvoke: false) };
        ChatMessageContent result;

        while (true)
            {
                result = await chatCompletion.GetChatMessageContentAsync(chatHistory, settings, kernel);

                // Check if the AI model has generated a response.
                if (result.Content is not null)
                {
                    Console.Write(result.Content);
                    // Sample output: "Considering the current weather conditions in Boston with a tornado watch in effect resulting in potential severe thunderstorms,
                    // the sky color is likely unusual such as green, yellow, or dark gray. Please stay safe and follow instructions from local authorities."
                    break;
                }

                // Adding AI model response containing chosen functions to chat history as it's required by the models to preserve the context.
                chatHistory.Add(result); 

                // Check if the AI model has chosen any function for invocation.
                IEnumerable<FunctionCallContent> functionCalls = FunctionCallContent.GetFunctionCalls(result);
                if (!functionCalls.Any())
                {
                    break;
                }

                // Sequentially iterating over each chosen function, invoke it, and add the result to the chat history.
                foreach (FunctionCallContent functionCall in functionCalls)
                {
                    try
                    {
                        // Invoking the function
                        FunctionResultContent resultContent = await functionCall.InvokeAsync(kernel);

                        // Adding the function result to the chat history                       
            
                        chatHistory.Add(resultContent.ToChatMessage());
                        
                        _logger.LogInformation("Extracted Content: {Content}",resultContent.Result);
                        _logger.LogInformation("Plugin Name: {Plugin}", resultContent.PluginName);

                        if(resultContent.PluginName=="customerdata")
                        {
                           var jsonString = resultContent.Result!.ToString();
                           
                           using (JsonDocument doc = JsonDocument.Parse(jsonString!))
                            {
                                                            if (doc.RootElement.TryGetProperty("paths", out JsonElement pathsElement) && pathsElement.ValueKind == JsonValueKind.Array)
                                {
                                    var paths = pathsElement.EnumerateArray()
                                                            .Select(p => p.GetString())
                                                            .Where(p => p != null)
                                                            .ToList();

                                    _logger.LogInformation("Paths: {Paths}", string.Join(", ", paths));
                                    
                               foreach(string path in paths!)
                                {

                                    chatHistory.AddUserMessage(
                                    [
                                        new ImageContent(await GetImagesAsBytes(path), "image/png"),
                                    ]);

                                }
                                }
                                else
                                {
                                    _logger.LogWarning("Paths property not found or is not an array.");
                                }                               
                            }
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        // Adding function exception to the chat history.
                        chatHistory.Add(new FunctionResultContent(functionCall, ex).ToChatMessage());
                        // or
                        //chatHistory.Add(new FunctionResultContent(functionCall, "Error details that the AI model can reason about.").ToChatMessage());
                    }
                }
            }

        string resp = string.Join(" ",result.Items);
        _logger.LogInformation("Response {response}",resp );
        //chatHistory.AddAssistantMessage(resp);

        return JsonSerializer.Serialize(new { resp });

        /*OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);

        string resp = string.Join(" ",response.Items);
        _logger.LogInformation("Response {response}",resp );
        chatHistory.AddAssistantMessage(resp);

        return JsonSerializer.Serialize(new { resp });

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
        byte[] bytes = System.Convert.FromBase64String(imageBase64List[0]);
        
        _logger.LogInformation("Getting result.");
        _logger.LogInformation("Customer: {customer}", customer);

        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage(
        [
            new ImageContent(bytes, "image/jpeg"),
        ]);

        _logger.LogInformation("images JSON: {images}", JsonSerializer.Serialize(imageBase64List));

        string? answer = await _chat.InvokeAsync<string>(_kernel, new()
        {
            { "customer", customer },
            { "documentation", context },
            { "question", question },
            //{ "images", JsonSerializer.Serialize(imageBase64List) }
        });


        _logger.LogInformation("Answer: {Answer}", answer);

        // Save the chat interaction to Cosmos DB
        //await _chatHistoryData.SaveChatAsync(customerId, question, answer);

        _logger.LogInformation("Chat interaction saved for customerId: {CustomerId}", customerId);*/


        
    }


    private async Task<byte[]> GetImagesAsBytes(string url)
    {       
            var blobClient = new BlobClient(new Uri(url),new DefaultAzureCredential());
            using var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            return stream.ToArray();
       
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