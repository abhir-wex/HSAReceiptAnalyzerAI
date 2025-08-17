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
            
            // Helper method to safely extract amount
            double GetAmountValue()
            {
                if (doc?.Fields.TryGetValue("Total", out var totalField) == true)
                {
                    try
                    {
                        // Try as Currency first
                        if (totalField.Value.AsCurrency() is CurrencyValue currencyValue)
                        {
                            return (double)currencyValue.Amount;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // If Currency fails, try as Double
                        try
                        {
                            return totalField.Value.AsDouble();
                        }
                        catch (InvalidOperationException)
                        {
                            // If both fail, try as String and parse
                            if (double.TryParse(totalField.Value.AsString(), out var parsedAmount))
                            {
                                return parsedAmount;
                            }
                        }
                    }
                }
                return 0.0;
            }

            string GetMerchantAddress()
            {
                if (doc?.Fields.TryGetValue("MerchantAddress", out var addressField) == true)
                {
                    try
                    {
                        // Try to get as Address first
                        var address = addressField.Value.AsAddress();
                        return address.ToString();
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            // Fallback to string if Address type fails
                            return addressField.Value.AsString();
                        }
                        catch (InvalidOperationException)
                        {
                            // Return empty string if both fail
                            return "";
                        }
                    }
                }
                return "";
            }
            // Helper method to generate receipt hash
            string GenerateReceiptHash()
            {
                var hashData = $"{GetAmountValue()}|" +
                              $"{(doc?.Fields.TryGetValue("MerchantName", out var merchantField) == true ? merchantField.Value.AsString() : "")}|" +
                              $"{(doc?.Fields.TryGetValue("TransactionDate", out var dateField) == true ? dateField.Value.AsDate().Date : DateTime.Now)}|" +
                              $"{string.Join(",", doc?.Fields.TryGetValue("Items", out var itemsField) == true ? itemsField.Value.AsList().Select(item => item.Value.AsDictionary().TryGetValue("Description", out var desc) ? desc.Value.AsString() : "") : new List<string>())}";
                
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashData));
                return Convert.ToHexString(hashBytes);
            }

            return new Claim
            {
                ClaimId = Guid.NewGuid().ToString(),
                ReceiptId = Guid.NewGuid().ToString(),
                Amount = GetAmountValue(),
                UserId = "USR0501", // Will need to be set from authenticated user context
                Name = "User_501", // Will need to be set from user profile
                Address = "123 Main St Apt 125, City, State, 94213", // Will need to be set from user profile
                Merchant = doc?.Fields.TryGetValue("MerchantName", out var merchantField) == true ? merchantField.Value.AsString() : "",
                ServiceType = "Medical", // Default for HSA
                DateOfService = doc?.Fields.TryGetValue("TransactionDate", out var dateField) == true ? dateField.Value.AsDate().Date : DateTime.Now,
                SubmissionDate = DateTime.Now,
                Category = "Healthcare", // Default for HSA
                Location = GetMerchantAddress(),
                UserAge = 0, // Will need to be set from user profile
                Items = doc?.Fields.TryGetValue("Items", out var itemsField) == true ? 
                    itemsField.Value.AsList().Select(item => 
                        item.Value.AsDictionary().TryGetValue("Description", out var desc) ? desc.Value.AsString() : "").ToList() : 
                    new List<string>(),
                UserGender = "Female", // Will need to be set from user profile
                Description = doc?.Fields.TryGetValue("MerchantName", out var descField) == true ? $"Receipt from {descField.Value.AsString()}" : "Medical expense",
                IsFraudulent = 0, // Default to not fraudulent
                FraudTemplate = "",
                Flags = "",
                IPAddress = "", // Will need to be set from request context
                ReceiptHash = GenerateReceiptHash()
            };
        }
    }
}

