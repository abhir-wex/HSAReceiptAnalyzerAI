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
    public float ReceiptHashDuplicateCount { get; set; }
    public float ReceiptHashFrequencyForUser { get; set; }
    public float ItemValidationScore { get; set; } // 0-100 score for HSA item eligibility
    public bool IsFraudulent { get; set; }
}