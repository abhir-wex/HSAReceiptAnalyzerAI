namespace HSAReceiptAnalyzer.Models
{
    public class SharedFraudReceiptSummary
    {
        public string Prompt { get; set; }
        public string Type { get; set; } = "SharedReceiptSummary";
        public List<SharedReceiptFraudResult> Results { get; set; }
        public string AiSummary { get; set; }
    }
}
