using HSAReceiptAnalyzer.Models;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
namespace HSAReceiptAnalyzer.Services
{
    public class FormRecognizerService
    {
        private readonly DocumentAnalysisClient _client;

        public FormRecognizerService(IConfiguration config)
        {
            var endpoint = new Uri(config["FormRecognizer:Endpoint"]);
            var credential = new AzureKeyCredential(config["FormRecognizer:Key"]);
            _client = new DocumentAnalysisClient(endpoint, credential);
        }

        public async Task<ReceiptData> ExtractDataAsync(IFormFile image)
        {
            using var stream = image.OpenReadStream();
            var result = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", stream);

            var doc = result.Value.Documents.FirstOrDefault();
            return new ReceiptData
            {
                Date = doc.Fields.TryGetValue("TransactionDate", out var date) ? date.Content : string.Empty,
                Amount = doc.Fields.TryGetValue("Total", out var amt) ? amt.Content : string.Empty,
                Merchant = doc.Fields.TryGetValue("MerchantName", out var merch) ? merch.Content : string.Empty,
                Description = doc.Fields.TryGetValue("Items", out var desc) ? desc.Content : string.Empty
            };
        }
    }
}

