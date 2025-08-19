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
        public string AIAnalysis { get; set; } = string.Empty; // AI-generated human-readable analysis
        
        // Item validation properties
        public float ItemValidationScore { get; set; } // 0-100 score for HSA item eligibility
        public string ItemValidationDetails { get; set; } = string.Empty; // Details about item validation
    }
}
