namespace HSAReceiptAnalyzer.Models
{
    public class FraudResult
    {
        public string ClaimId { get; set; }
        public bool IsFraudulent { get; set; }
        public float MlScore { get; set; }
        public float RuleScore { get; set; }
        public float FinalFraudScore { get; set; }
        public string Explanation { get; set; }
    }
}
