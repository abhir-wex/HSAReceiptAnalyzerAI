namespace HSAReceiptAnalyzer.Models
{
    public class SharedReceiptFraudResult
    {
        public string ReceiptId { get; set; }
        public List<string> Users { get; set; }
        public string FraudTemplate { get; set; }
    }
}
