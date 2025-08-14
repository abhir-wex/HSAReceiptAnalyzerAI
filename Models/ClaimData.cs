using Microsoft.ML.Data;

namespace HSAReceiptAnalyzer.Models
{
    
        public class ClaimData
        {
            public float Amount { get; set; }
            public float DaysSinceLastClaim { get; set; }
            public float VendorFrequency { get; set; }
            public float DistinctVendors { get; set; }
        }

        public class ClaimPrediction
        {
            [VectorType(3)]
            public double[] Score { get; set; }
        }
    }


