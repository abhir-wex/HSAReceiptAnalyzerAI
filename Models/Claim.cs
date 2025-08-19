using Microsoft.ML.Data;

namespace HSAReceiptAnalyzer.Models
{
      public class Claim
        {
            public string ClaimId { get; set; }
            public string ReceiptId { get; set; }
            public string UserId { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Merchant { get; set; }
            public string ServiceType { get; set; }
            public double Amount { get; set; }
            public DateTime DateOfService { get; set; }
            public DateTime SubmissionDate { get; set; }
            public string Category { get; set; }
            public string Location { get; set; }
            public int UserAge { get; set; }
            public List<string> Items {  get; set; }
            public string UserGender { get; set; }
            public string Description { get; set; }
            public int IsFraudulent { get; set; }
            public string FraudTemplate { get; set; }
            public string Flags { get; set; }
            public string VendorId { get; set; }
            public string IPAddress { get; set; }
            public string ReceiptHash { get; set; }
            
            // Item validation properties for HSA eligibility scoring
            public float ItemValidationScore { get; set; } // 0-100 score based on how well items match allowed HSA list
            public List<string> ValidItems { get; set; } = new List<string>(); // Items that match HSA-eligible list
            public List<string> InvalidItems { get; set; } = new List<string>(); // Items that don't match HSA-eligible list
            public string ItemValidationNotes { get; set; } // Additional notes about item validation
        }

    public class ClaimFeatures
    {
        public float Amount { get; set; }
        public float DaysSinceLastClaim { get; set; }
        public float SubmissionDelayDays { get; set; }
        public float VendorFrequency { get; set; }
        public float CategoryFrequency { get; set; }
        public float AverageClaimAmountForUser { get; set; }
        public float AmountDeviationFromAverage { get; set; }
        public float IPAddressChangeFrequency { get; set; }
        public float ItemCount { get; set; }
        public float DistinctItemsRatio { get; set; }
        public float UserAge { get; set; }
        public bool IsFraudulent { get; set; }
    }

    // ML.NET RandomizedPCA prediction output
    public class AnomalyPrediction
    {
        // true => anomaly
        [ColumnName("PredictedLabel")]
        public bool IsAnomaly { get; set; }

        // Larger usually means "more anomalous"
        public float Score { get; set; }
    }
}
