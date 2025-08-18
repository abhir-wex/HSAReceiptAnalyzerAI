using HSAReceiptAnalyzer.Models;

namespace HSAReceiptAnalyzer.Data
{
    public class ClaimTransformer
    {
        public static Claim Transform(ExpenseDetail expenseDetail)
        {
            // Generate random data for fields not in the source
            var random = new Random();

            return new Claim
            {
                ClaimId = expenseDetail.ClaimId.ToString(),
                ReceiptId = expenseDetail.ReceiptId.ToString(),
                UserId = "USR" + expenseDetail.UserId.ToString("D4"),
                Name = expenseDetail.Name,
                Address = $"{random.Next(100, 999)} Main St, {expenseDetail.Location}",
                Merchant = expenseDetail.Merchant,
                ServiceType = expenseDetail.ServiceType,
                Amount = (double)expenseDetail.Amount,
                DateOfService = expenseDetail.DateOfService,
                SubmissionDate = expenseDetail.SubmissionDate,
                Category = expenseDetail.Category,
                Location = expenseDetail.Location,
                UserAge = random.Next(18, 70), // Generate random age
                Items = !string.IsNullOrEmpty(expenseDetail.CategoryDescription)
                            ? expenseDetail.CategoryDescription.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                            : new List<string>(),
                UserGender = (random.Next(0, 3)) switch { 0 => "Male", 1 => "Female", _ => "Other" },
                Description = $"Claim for {expenseDetail.Category}",
                IsFraudulent = 0,
                FraudTemplate = "",
                Flags = "",
                VendorId = "VND" + expenseDetail.VendorName,
                IPAddress = $"{random.Next(1, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}.{random.Next(1, 254)}",
                ReceiptHash = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper()
            };
        }
    }
}
