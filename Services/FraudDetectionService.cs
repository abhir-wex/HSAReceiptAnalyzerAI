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
        private readonly ITransformer _model;
        private readonly PredictionEngine<ClaimFeatures, FraudPrediction> _predictionEngine;

        public FraudDetectionService(IClaimDatabaseManager claimDatabaseManager) {

            _claimDatabaseManager = claimDatabaseManager;
            _mlContext = new MLContext();

            var modelPath = Path.Combine(AppContext.BaseDirectory, "fraudModel.zip");
            using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _model = _mlContext.Model.Load(stream, out var modelInputSchema);

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ClaimFeatures, FraudPrediction>(_model);
        }

        public FraudResult Predict(Claim claim)
        {
            // Convert claim to features
            var claimFeatures = MapToClaimFeatures(claim, includeLabel: false);

            // Rule check: duplicate receipt across users
            bool duplicateFound = _claimDatabaseManager.ExistsDuplicate(claim.ReceiptHash, claim.UserId);

            // Run ML prediction (supervised)
            var mlPrediction = _predictionEngine.Predict(claimFeatures);

            // ML fraud score as %
            float mlScore = mlPrediction.Probability * 100;

            // Rule score if duplicate
            float ruleScore = duplicateFound ? 95f : 0f;

            // Final score: whichever is stronger
            float finalScore = Math.Max(mlScore, ruleScore);

            // Fraud decision: either ML thinks fraudulent or rules triggered
            bool isFraudulent = mlPrediction.IsFraudulent || duplicateFound || finalScore >= 75;

            // Build result
            return new FraudResult
            {
                ClaimId = claim.ClaimId,
                IsFraudulent = isFraudulent,
                MlScore = mlScore,
                RuleScore = ruleScore,
                FinalFraudScore = finalScore,
                Explanation = duplicateFound
                    ? "⚠ Duplicate receipt hash found across users."
                    : (mlPrediction.IsFraudulent
                        ? $"⚠ ML classified as fraudulent ({mlScore:F2}%)."
                        : "✅ Claim appears normal.")
            };
        }

        public string TrainModel()
        {
            var claims = _claimDatabaseManager.GetAllClaims().ToList();
            if (!claims.Any())
                throw new InvalidOperationException("No claims available for training.");

            bool includeLabel = true;

            // Convert raw claims to ClaimFeatures (must include IsFraud label)
            var claimFeaturesList = claims.Select(c => MapToClaimFeatures(c, includeLabel)).ToList();

            // Load into ML.NET
            var trainingData = _mlContext.Data.LoadFromEnumerable(claimFeaturesList);

            // Collect all float features except the label
            var featureColumns = typeof(ClaimFeatures)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(float))
                .Select(p => p.Name)
                .ToArray();

            // Replace the pipeline definition with the following:
            var pipeline = _mlContext.Transforms.Concatenate("Features", featureColumns)
                .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                    labelColumnName: nameof(ClaimFeatures.IsFraudulent),
                    featureColumnName: "Features"));

            // Train model
            var model = pipeline.Fit(trainingData);

            // Save model to file
            var modelPath = Path.Combine(AppContext.BaseDirectory, "fraudModel.zip");
            _mlContext.Model.Save(model, trainingData.Schema, modelPath);

            return modelPath;
        }

        private ClaimFeatures MapToClaimFeatures(Claim claim, bool includeLabel = false)
        {
            var features = new ClaimFeatures
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
                ReceiptHashDuplicateCount = CalculateReceiptHashDuplicateCount(claim.ReceiptHash),
                ReceiptHashFrequencyForUser = CalculateReceiptHashFrequencyForUser(claim.UserId, claim.ReceiptHash),
                //UserAge = CalculateUserAge(claim.UserId),
               // IsFraudulent = claim.IsFraudulent == 1
            };

            if (includeLabel)
            {
                // Only used during training
                features.IsFraudulent = claim.IsFraudulent == 1;
            }

            return features;
        }


        private float CalculateReceiptHashDuplicateCount(string receiptHash)
        {
            if (string.IsNullOrEmpty(receiptHash)) return 0f;

            var allClaims = _claimDatabaseManager.GetAllClaims();
            var duplicateCount = allClaims.Count(c =>
                !string.IsNullOrEmpty(c.ReceiptHash) &&
                string.Equals(c.ReceiptHash, receiptHash, StringComparison.OrdinalIgnoreCase));

            // Return count - 1 to exclude the current claim itself
            return Math.Max(0f, duplicateCount - 1);
        }

        private float CalculateReceiptHashFrequencyForUser(string userId, string receiptHash)
        {
            if (string.IsNullOrEmpty(receiptHash)) return 0f;

            var userClaims = _claimDatabaseManager.GetClaims(userId);
            if (!userClaims.Any()) return 0f;

            var hashMatchCount = userClaims.Count(c =>
                !string.IsNullOrEmpty(c.ReceiptHash) &&
                string.Equals(c.ReceiptHash, receiptHash, StringComparison.OrdinalIgnoreCase));

            return (float)hashMatchCount / userClaims.Count();
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

