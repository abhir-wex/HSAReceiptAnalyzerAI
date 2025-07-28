using HSAReceiptAnalyzer.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
namespace HSAReceiptAnalyzer.Services
{
    public class SemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly KernelFunction _fraudFunction;

        public SemanticKernelService(IConfiguration config)
        {
            _kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                deploymentName: config["AzureOpenAI:Deployment"],
                endpoint: config["AzureOpenAI:Endpoint"],
                apiKey: config["AzureOpenAI:Key"])
            .Build();

            _fraudFunction = _kernel.CreateFunctionFromPrompt(@"
You are a healthcare claims fraud analyst. Given the following HSA receipt:

Date: {{$Date}}
Amount: {{$Amount}}
Merchant: {{$Merchant}}
Description: {{$Description}}

Classify this claim as 'Valid' or 'Fraudulent' and give a short reason why.
");
        }

        //public async Task<string> AnalyzeReceiptAsync(ReceiptData data)
        //{
        //    var result = await _fraudFunction.InvokeAsync(_kernel, new KernelArguments
        //    {
        //        ["Date"] = data.Date,
        //        ["Amount"] = data.Amount,
        //        ["Merchant"] = data.Merchant,
        //        ["Description"] = data.Description
        //    });

        //    return result?.ToString();
        //}

        public async Task<string> AnalyzeReceiptAsync(ReceiptData data)
        {
            var arguments = new KernelArguments
            {
                ["Date"] = data.Date,
                ["Amount"] = data.Amount,
                ["Merchant"] = data.Merchant,
                ["Description"] = data.Description
            };

            var result = await _fraudFunction.InvokeAsync(_kernel, arguments);
            return result.ToString();
        }
    }
    }


