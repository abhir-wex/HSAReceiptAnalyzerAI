using DotNetEnv;
using HSAReceiptAnalyzer.Controllers;
using HSAReceiptAnalyzer.Data;
using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.Data.Sqlite;
using Microsoft.ML;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ClaimDatabaseOptions>(options =>
{
    options.DbPath = "ClaimsDB1.sqlite";
    options.JsonPath = "Data/multiple_users.json";
});

Env.Load();
var kernelBuilder = Kernel.CreateBuilder();

// WEX Gateway configuration for Semantic Kernel
string modelId = "azure-gpt-4o";

// Get configuration from appsettings.json instead of environment variables
var wexConfig = builder.Configuration.GetSection("WEXOpenAI");
string endpoint = wexConfig["Endpoint"] ?? 
    Environment.GetEnvironmentVariable("WEX_OPENAI_ENDPOINT") ?? 
    throw new InvalidOperationException("WEX OpenAI endpoint not configured. Please set WEXOpenAI:Endpoint in appsettings.json or WEX_OPENAI_ENDPOINT environment variable.");

string apiKey = wexConfig["Key"] ?? 
    Environment.GetEnvironmentVariable("WEX_OPENAI_KEY") ?? 
    throw new InvalidOperationException("WEX OpenAI API key not configured. Please set WEXOpenAI:Key in appsettings.json or WEX_OPENAI_KEY environment variable.");

// Validate that we have actual values (not empty strings)
if (string.IsNullOrWhiteSpace(endpoint))
    throw new InvalidOperationException("WEX OpenAI endpoint is empty. Please configure WEXOpenAI:Endpoint in appsettings.json");

if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException("WEX OpenAI API key is empty. Please configure WEXOpenAI:Key in appsettings.json");

kernelBuilder.AddOpenAIChatCompletion(
    modelId: modelId,
    apiKey: apiKey,
    endpoint: new Uri(endpoint)
);

var kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);

// Register all services
builder.Services.AddScoped<ISemanticKernelService, SemanticKernelService>();
builder.Services.AddScoped<IFormRecognizerService, FormRecognizerService>();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<IRAGService, RAGService>();

builder.Services.AddScoped<IClaimDatabaseManager>(provider =>
{
    var dbPath = "ClaimsDB1.sqlite";
    var jsonPath = "Data/multiple_users.json";
    var connection = $"Data Source={dbPath}";
    return new ClaimDatabaseManager(dbPath, jsonPath, connection);
});

SQLitePCL.Batteries.Init();
builder.Services.AddSingleton<MLContext>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.UseAuthorization();
app.MapControllers();

// Initialize RAG Knowledge Base on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var ragService = scope.ServiceProvider.GetRequiredService<IRAGService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Initializing RAG Knowledge Base...");
        await ragService.InitializeKnowledgeBaseAsync();
        logger.LogInformation("RAG Knowledge Base initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize RAG Knowledge Base");
        // Continue startup even if RAG initialization fails
    }
}

app.Run();