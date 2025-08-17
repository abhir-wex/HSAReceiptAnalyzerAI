using Microsoft.ML.Data;

namespace HSAReceiptAnalyzer.Models
{
    public class FraudPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool IsFraudulent { get; set; }   // true = fraud, false = normal

        [ColumnName("Probability")]
        public float Probability { get; set; }   // confidence score (0–1)

        [ColumnName("Score")]
        public float Score { get; set; }         // raw score from LightGBM
    }
}
