using HSAReceiptAnalyzer.Models;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using HSAReceiptAnalyzer.Services.Interface;
namespace HSAReceiptAnalyzer.Services
{
    public class FormRecognizerService : IFormRecognizerService
    {
        private readonly DocumentAnalysisClient _client;

        public FormRecognizerService(IConfiguration config)
        {
            var endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_ENDPOINT")) ;// new Uri(config["FormRecognizer:Endpoint"]);
            var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_KEY")); // new AzureKeyCredential(config["FormRecognizer:Key"]);
            _client = new DocumentAnalysisClient(endpoint, credential);
        }

        public async Task<Claim> ExtractDataAsync(IFormFile image)
        {
            using var stream = image.OpenReadStream();
            var result = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", stream);

            var doc = result.Value.Documents.FirstOrDefault();
            return new Claim
            {
                ClaimId = Guid.NewGuid().ToString(),
                ReceiptId = Guid.NewGuid().ToString(),
                Amount = doc?.Fields.TryGetValue("Total", out var totalField) == true && totalField.Value.AsCurrency() is CurrencyValue currencyValue ? (double)currencyValue.Amount : 0.0,
                UserId = "", // Will need to be set from authenticated user context
                Name = "", // Will need to be set from user profile
                Address = "", // Will need to be set from user profile
                Merchant = doc?.Fields.TryGetValue("MerchantName", out var merchantField) == true ? merchantField.Value.AsString() : "",
                //Amount = doc?.Fields.TryGetValue("Total", out var totalField) == true && totalField.Value.AsCurrency() is CurrencyValue currencyValue ? (decimal)currencyValue.Amount : 0m,
                ServiceType = "Medical", // Default for HSA
                DateOfService = doc?.Fields.TryGetValue("TransactionDate", out var dateField) == true ? dateField.Value.AsDate().Date : DateTime.Now,
                //Amount = doc?.Fields.TryGetValue("Total", out var totalField) == true && totalField.Value.AsCurrency() is CurrencyValue currencyValue ? (decimal)currencyValue.Amount : 0m,
                
                SubmissionDate = DateTime.Now,
                Category = "Healthcare", // Default for HSA
                Location = doc?.Fields.TryGetValue("MerchantAddress", out var addressField) == true ? addressField.Value.AsString() : "",
                UserAge = 0, // Will need to be set from user profile
                Items = doc?.Fields.TryGetValue("Items", out var itemsField) == true ? 
                    itemsField.Value.AsList().Select(item => 
                        item.Value.AsDictionary().TryGetValue("Description", out var desc) ? desc.Value.AsString() : "").ToList() : 
                    new List<string>(),
                UserGender = "", // Will need to be set from user profile
                Description = doc?.Fields.TryGetValue("MerchantName", out var descField) == true ? $"Receipt from {descField.Value.AsString()}" : "Medical expense",
                IsFraudulent = 0, // Default to not fraudulent
                FraudTemplate = "",
                Flags = "",
                IPAddress = "" // Will need to be set from request context
            };
            //write an insert method to save in claims table
        }
    }
}

