using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.ML;
using System.Security.Claims;
using Claim = HSAReceiptAnalyzer.Models.Claim;

namespace HSAReceiptAnalyzer.Services
{
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly IClaimDatabaseManager _claimDatabaseManager;
        private readonly MLContext _mlContext;

        public FraudDetectionService(IClaimDatabaseManager claimDatabaseManager) {

            _claimDatabaseManager = claimDatabaseManager;
            _mlContext = new MLContext();
        }

        public string TrainModel()
        {
            var claims = _claimDatabaseManager.GetAllClaims().ToList();
            if (!claims.Any())
                throw new InvalidOperationException("No claims available for training.");

            // Convert raw claims to ClaimFeatures
            var claimFeaturesList = claims.Select(c => MapToClaimFeatures(c)).ToList();

            var featureColumns = typeof(ClaimFeatures)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(float))
                .Select(p => p.Name)
                .ToArray();

            var trainingData = _mlContext.Data.LoadFromEnumerable(claimFeaturesList);

            var pipeline = _mlContext.Transforms.Concatenate("Features", featureColumns)
                .Append(_mlContext.AnomalyDetection.Trainers.RandomizedPca("Features", rank: 5));

            var model = pipeline.Fit(trainingData);

            var modelPath = Path.Combine(AppContext.BaseDirectory, "fraudModel.zip");
            _mlContext.Model.Save(model, trainingData.Schema, modelPath);

            return modelPath;
        }

        private ClaimFeatures MapToClaimFeatures(Claim claim)
        {
            return new ClaimFeatures
            {
                Amount = (float)claim.Amount,
                DaysSinceLastClaim = CalculateDaysSinceLastClaim(claim.UserId, claim.DateOfService),
                SubmissionDelayDays = (float)(claim.SubmissionDate - claim.DateOfService).TotalDays,
                VendorFrequency = CalculateVendorFrequency(claim.UserId, claim.VendorId),
                CategoryFrequency = CalculateCategoryFrequency(claim.UserId, claim.Category),
                AverageClaimAmountForUser = CalculateAverageAmountForUser(claim.UserId),
                AmountDeviationFromAverage = CalculateAmountDeviation(claim.UserId, claim.Amount),
                IPAddressChangeFrequency = CalculateIPChangeFrequency(claim.UserId),
                ItemCount = claim.Items.Count,
                DistinctItemsRatio = CalculateDistinctItemRatio(claim.Items),
                //UserAge = CalculateUserAge(claim.UserId),
                IsFraudulent = false
            };
        }

        private float CalculateDaysSinceLastClaim(string userId, DateTime claimDate)
        {
            var previousClaim = _claimDatabaseManager
                .GetClaims(userId)
                .Where(c => c.SubmissionDate < claimDate)
                .OrderByDescending(c => c.SubmissionDate)
                .FirstOrDefault();

            if (previousClaim == null) return 9999f; // No prior claim

            return (float)(claimDate - previousClaim.SubmissionDate).TotalDays;
        }

        private float CalculateVendorFrequency(string userId, string vendorId)
        {
            var claims = _claimDatabaseManager.GetClaims(userId);
            if (!claims.Any()) return 0f;

            var vendorClaims = claims.Count(c => c.VendorId == vendorId);
            return (float)vendorClaims / claims.Count();
        }

        private float CalculateCategoryFrequency(string userId, string category)
        {
            var claims = _claimDatabaseManager.GetClaims(userId);
            if (!claims.Any()) return 0f;

            var categoryClaims = claims.Count(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
            return (float)categoryClaims / claims.Count();
        }

        private float CalculateAverageAmountForUser(string userId)
        {
            var claims = _claimDatabaseManager.GetClaims(userId);
            if (!claims.Any()) return 0f;

            return (float)claims.Average(c => c.Amount);
        }

        private float CalculateAmountDeviation(string userId, double amount)
        {
            var avg = CalculateAverageAmountForUser(userId);
            if (avg == 0) return 0f;

            return (float)Math.Abs(amount - avg) / avg;
        }

        private float CalculateIPChangeFrequency(string userId)
        {
            var claims = _claimDatabaseManager.GetClaims(userId)
                .OrderBy(c => c.SubmissionDate)
                .ToList();

            if (claims.Count <= 1) return 0f;

            int changeCount = 0;
            string lastIP = claims.First().IPAddress;
            foreach (var c in claims.Skip(1))
            {
                if (!string.Equals(c.IPAddress, lastIP, StringComparison.OrdinalIgnoreCase))
                    changeCount++;

                lastIP = c.IPAddress;
            }

            return (float)changeCount / (claims.Count - 1);
        }

        private float CalculateDistinctItemRatio(List<string> items)
        {
            if (items == null || !items.Any()) return 0f;

            var distinctCount = items.Select(i => i).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            return (float)distinctCount / items.Count;
        }

        //private float CalculateUserAge(string userId)
        //{
        //    var user = _claimDatabaseManager.GetClaims(userId);
        //    if (user?.DateOfBirth == null) return 0f;

        //    var age = DateTime.UtcNow.Year - user.DateOfBirth.Value.Year;
        //    if (DateTime.UtcNow.Date < user.DateOfBirth.Value.AddYears(age))
        //        age--;

        //    return (float)age;
        //}


    }


}

