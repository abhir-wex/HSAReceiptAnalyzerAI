using HSAReceiptAnalyzer.Models;

namespace HSAReceiptAnalyzer.Services
{
    public class ClaimFeatureExtractor
    {
        public ClaimFeatures ExtractFeatures(Claim claim, List<Claim> userHistory)
        {
            var daysSinceLastClaim = userHistory
                .Where(c => c.ClaimId != claim.ClaimId)
                .OrderByDescending(c => c.SubmissionDate)
                .Select(c => (claim.SubmissionDate - c.SubmissionDate).TotalDays)
                .FirstOrDefault();

            var avgAmount = userHistory.Average(c => c.Amount);
            var amountDeviation = (float)Math.Abs((double)(claim.Amount - avgAmount));

            var vendorFrequency = userHistory.Count(c => c.Merchant == claim.Merchant);
            var categoryFrequency = userHistory.Count(c => c.Category == claim.Category);

            var ipChangeFrequency = userHistory.Select(c => c.IPAddress).Distinct().Count();

            return new ClaimFeatures
            {
                Amount = (float)claim.Amount,
                DaysSinceLastClaim = (float)daysSinceLastClaim,
                SubmissionDelayDays = (float)(claim.SubmissionDate - claim.DateOfService).TotalDays,
                VendorFrequency = vendorFrequency,
                CategoryFrequency = categoryFrequency,
                AverageClaimAmountForUser = (float)avgAmount,
                AmountDeviationFromAverage = amountDeviation,
                IPAddressChangeFrequency = ipChangeFrequency,
                ItemCount = claim.Items?.Count ?? 0,
                DistinctItemsRatio = claim.Items != null && claim.Items.Count > 0
                    ? (float)claim.Items.Distinct().Count() / claim.Items.Count
                    : 0f,
                UserAge = claim.UserAge,
                IsFraudulent = claim.IsFraudulent == 1
            };
        }
    }
}
