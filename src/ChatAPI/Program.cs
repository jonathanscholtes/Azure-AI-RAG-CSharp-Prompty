using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents;
using ChatAPI.Data;
using ChatAPI.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel;
using Azure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(serviceProvider => new CosmosClient(builder.Configuration["CosmosDb:ConnectionString"]));

builder.Services.AddSingleton(serviceProvider => new OpenAIClient(new Uri(builder.Configuration["OpenAi:endpoint"]!), new AzureKeyCredential(builder.Configuration["OpenAi:Key"]!)));
builder.Services.AddKernel();
builder.Services.AddAzureOpenAIChatCompletion(builder.Configuration["OpenAi:deployment"]!);
builder.Services.AddAzureOpenAITextEmbeddingGeneration(builder.Configuration["OpenAi:embedding_deployment"]!);

builder.Services.AddSingleton(serviceProvider => new SearchClient(
    new Uri(builder.Configuration["AzureAISearch:Endpoint"]!),
    builder.Configuration["AzureAISearch:index_name"],
    new AzureKeyCredential(builder.Configuration["AzureAISearch:Key"]!)));

builder.Services.AddSingleton(serviceProvider => new SearchIndexClient(
    new Uri(builder.Configuration["AzureAISearch:Endpoint"]!),
    new AzureKeyCredential(builder.Configuration["AzureAISearch:Key"]!)));

builder.Services.AddScoped<CustomerData>();
builder.Services.AddSingleton<GenerateCustomerInfo>();
//builder.Services.AddSingleton<GenerateProductInfo>();
builder.Services.AddScoped<AISearchData>();
builder.Services.AddScoped<ChatService>();

//Application Insights
builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.Credential = new DefaultAzureCredential();
});

var app = builder.Build();

// Make sure database and search index are populated with data and application is in a good startup state
await PopulateData(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();


async Task PopulateData(IServiceProvider serviceProvider)
{
    var customerData = serviceProvider.GetRequiredService<GenerateCustomerInfo>();
    //var aiSearchData = serviceProvider.GetRequiredService<GenerateProductInfo>();

    await Task.WhenAll(
        customerData.PopulateCosmosAsync()
       // aiSearchData.PopulateSearchIndexAsync()
    );
}
