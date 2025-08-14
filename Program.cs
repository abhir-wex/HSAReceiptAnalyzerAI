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
var kernel = kernelBuilder.Build();

builder.Services.AddSingleton(kernel);

builder.Services.AddScoped<ISemanticKernelService, SemanticKernelService>();
builder.Services.AddScoped<IFormRecognizerService, FormRecognizerService>();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();

builder.Services.AddScoped<IClaimDatabaseManager>(provider =>
{
    var dbPath = "ClaimsDB1.sqlite";
    var jsonPath = "Data/multiple_users.json";
    var connection = $"Data Source={dbPath}";
    return new ClaimDatabaseManager(dbPath, jsonPath, connection);
});
SQLitePCL.Batteries.Init();

builder.Services.AddSingleton<MLContext>();

//builder.Services.AddPredictionEnginePool<Claim, AnomalyPrediction>();

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

// Add endpoint to manually trigger model retraining
app.MapPost("/retrain-model", (IServiceProvider serviceProvider) =>
{
    try
    {
        var dbPath = "ClaimsDB1.sqlite";
        var modelPath = "Models/fraud-detection-model.zip";
        
        Directory.CreateDirectory("Models");
        TrainAnomalyModel.Run(dbPath, modelPath);
        
        return Results.Ok("Model retrained successfully. Restart the application to use the new model.");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Failed to retrain model: {ex.Message}");
    }
});

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.UseAuthorization();
app.MapControllers();

app.Run();




