namespace HSAReceiptAnalyzer.Models
{
    public class ImageUploadRequest
    {
        public IFormFile Image { get; set; }
        public string UserId { get; set; }
        public string Date { get; set; }
        public decimal Amount { get; set; }  // Consider using decimal for monetary values
        public string Merchant { get; set; }
        public string Description { get; set; }
        public string CustomPrompt { get; set; }
    }
}
