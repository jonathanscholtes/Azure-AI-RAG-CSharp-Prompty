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


public sealed class ChatService(Kernel kernel, ITextEmbeddingGenerationService embedding, CustomerData customerData,ChatHistory chatHistory, AISearchData aiSearch, ILogger<ChatService> logger)
{
    private readonly CustomerData _customerData = customerData;
    private readonly AISearchData _aiSearch = aiSearch;
    private readonly ILogger<ChatService> _logger = logger;

    private readonly Kernel _kernel = kernel;
    private readonly ITextEmbeddingGenerationService _embedding = embedding;
    //private readonly KernelFunction _chat = kernel.CreateFunctionFromPromptyFile("chat.prompty");

    //private readonly KernelFunction _chat = kernel.CreateFunctionFromPromptYaml(File.ReadAllText("chat.yaml"), promptTemplateFactory: new HandlebarsPromptTemplateFactory());

    private readonly ChatHistory _chatHistory = chatHistory;

    public async Task<string> GetResponseAsync( string question)
    {
         _chatHistory.AddUserMessage(question);

        IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        PromptExecutionSettings settings = new() { FunctionChoiceBehavior = Microsoft.SemanticKernel.FunctionChoiceBehavior.Auto(autoInvoke: false) };
        ChatMessageContent result;

        while (true)
            {
                result = await chatCompletion.GetChatMessageContentAsync(_chatHistory, settings, kernel);

                // Check if the AI model has generated a response.
                if (result.Content is not null)
                {
                    Console.Write(result.Content);
                    // Sample output: "Considering the current weather conditions in Boston with a tornado watch in effect resulting in potential severe thunderstorms,
                    // the sky color is likely unusual such as green, yellow, or dark gray. Please stay safe and follow instructions from local authorities."
                    break;
                }

                // Adding AI model response containing chosen functions to chat history as it's required by the models to preserve the context.
                _chatHistory.Add(result); 

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

                        FunctionResultContent resultContent = await functionCall.InvokeAsync(kernel);


                        _chatHistory.Add(resultContent.ToChatMessage());
                        
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

                                    _chatHistory.AddUserMessage(
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
                        _chatHistory.Add(new FunctionResultContent(functionCall, ex).ToChatMessage());
                    }
                }
            }

        string resp = string.Join(" ",result.Items);
        _logger.LogInformation("Response {response}",resp );
        _chatHistory.AddAssistantMessage(resp);

        return JsonSerializer.Serialize(new { resp });
 
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