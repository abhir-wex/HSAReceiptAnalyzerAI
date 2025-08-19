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
        private readonly ISemanticKernelService _semanticKernelService;
        private readonly MLContext _mlContext;
        private readonly ITransformer _model;
        private readonly PredictionEngine<ClaimFeatures, FraudPrediction> _predictionEngine;

        public FraudDetectionService(IClaimDatabaseManager claimDatabaseManager, ISemanticKernelService semanticKernelService) {

            _claimDatabaseManager = claimDatabaseManager;
            _semanticKernelService = semanticKernelService;
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

            // Rule check 1: duplicate receipt across users
            bool duplicateFound = _claimDatabaseManager.ExistsDuplicate(claim.ReceiptHash, claim.UserId);

            // Rule check 2: invalid HSA items detection
            bool invalidItemsDetected = false;
            string invalidItemsReason = "";
            
            // Check for extremely low validation scores
            if (claim.ItemValidationScore < 20)
            {
                invalidItemsDetected = true;
                invalidItemsReason = $"Extremely low item validation score ({claim.ItemValidationScore:F1}%)";
            }
            // Check for high ratio of invalid items
            else if (claim.InvalidItems?.Count > 0 && claim.Items?.Count > 0)
            {
                var invalidRatio = (float)claim.InvalidItems.Count / claim.Items.Count;
                if (invalidRatio >= 0.7f) // 70% or more invalid items
                {
                    invalidItemsDetected = true;
                    invalidItemsReason = $"High invalid items ratio ({invalidRatio:P0})";
                }
            }
            // Check for clearly prohibited items
            var prohibitedItems = new[] { "alcohol", "beer", "wine", "cigarettes", "tobacco", "candy", "soda", "chips", "cosmetics" };
            var foundProhibited = claim.InvalidItems?.Where(item => 
                prohibitedItems.Any(prohibited => item.Contains(prohibited, StringComparison.OrdinalIgnoreCase))
            ).ToList() ?? new List<string>();
            
            if (foundProhibited.Any())
            {
                invalidItemsDetected = true;
                invalidItemsReason = $"Contains prohibited items: {string.Join(", ", foundProhibited)}";
            }

            // Run ML prediction (supervised)
            var mlPrediction = _predictionEngine.Predict(claimFeatures);

            // ML fraud score as %
            float mlScore = mlPrediction.Probability * 100;

            // Rule scores
            float duplicateRuleScore = duplicateFound ? 95f : 0f;
            float invalidItemsRuleScore = invalidItemsDetected ? 85f : 0f;

            // Item validation score (0-100, where lower scores indicate potential fraud)
            float itemValidationScore = claim.ItemValidationScore;
            
            // Convert item validation score to fraud risk (inverse relationship)
            float itemFraudScore = 0f;
            if (itemValidationScore < 30)
            {
                itemFraudScore = 80f; // High fraud risk for very low validation scores
            }
            else if (itemValidationScore < 50)
            {
                itemFraudScore = 60f; // Medium fraud risk for low validation scores
            }
            else if (itemValidationScore < 70)
            {
                itemFraudScore = 30f; // Low fraud risk for moderate validation scores
            }
            // else itemFraudScore remains 0 for high validation scores (70+)

            // Final score: take the maximum of all fraud detection methods
            float finalScore = Math.Max(Math.Max(Math.Max(mlScore, duplicateRuleScore), invalidItemsRuleScore), itemFraudScore);

            // Fraud decision: fraud if any detection method triggers
            bool isFraudulent = mlPrediction.IsFraudulent || duplicateFound || invalidItemsDetected || finalScore >= 75 || itemValidationScore < 30;

            // Build comprehensive explanation with priority order
            string explanation = "";
            if (duplicateFound)
            {
                explanation = "⚠ Duplicate receipt hash found across users.";
            }
            else if (invalidItemsDetected)
            {
                explanation = $"⚠ Invalid HSA items detected: {invalidItemsReason}";
            }
            else if (itemValidationScore < 30)
            {
                explanation = $"⚠ Poor item validation score ({itemValidationScore:F1}%) - contains non-HSA eligible items.";
            }
            else if (mlPrediction.IsFraudulent)
            {
                explanation = $"⚠ ML classified as fraudulent ({mlScore:F2}%).";
            }
            else if (itemValidationScore < 70)
            {
                explanation = $"⚠ Moderate item validation concerns ({itemValidationScore:F1}%) combined with other risk factors.";
            }
            else
            {
                explanation = "✅ Claim appears normal.";
            }

            // Build result with enhanced rule scoring
            return new FraudResult
            {
                ClaimId = claim.ClaimId,
                IsFraudulent = isFraudulent,
                MlScore = mlScore,
                RuleScore = Math.Max(duplicateRuleScore, invalidItemsRuleScore), // Highest rule score
                FinalFraudScore = finalScore,
                Explanation = explanation,
                // Add additional context about item validation
                ItemValidationScore = itemValidationScore,
                ItemValidationDetails = !string.IsNullOrEmpty(claim.ItemValidationNotes) ? claim.ItemValidationNotes : 
                    (invalidItemsDetected ? $"Item validation failed: {invalidItemsReason}" : "")
            };
        }

        public async Task<FraudResult> PredictWithAIAnalysisAsync(Claim claim)
        {
            // Get the standard ML prediction
            var standardResult = Predict(claim);

            try
            {
                // Generate AI analysis using SemanticKernelService
                var aiAnalysis = await _semanticKernelService.AnalyzeReceiptAsync(claim);
                
                // Add AI analysis to the result
                standardResult.AIAnalysis = aiAnalysis;
            }
            catch (Exception ex)
            {
                // If AI analysis fails, use fallback message
                standardResult.AIAnalysis = $"AI analysis unavailable: {ex.Message}";
            }

            return standardResult;
        }

        public string TrainModel()
        {
            var claims = _claimDatabaseManager.GetAllClaims().ToList();
            if (!claims.Any())
                throw new InvalidOperationException("No claims available for training.");

            // OPTIMIZATION 1: Pre-calculate aggregated data once
            var userClaimsLookup = claims.GroupBy(c => c.UserId).ToLookup(g => g.Key, g => g.ToList());
            var receiptHashLookup = claims.GroupBy(c => c.ReceiptHash).ToLookup(g => g.Key, g => g.Count());
            var userAverages = userClaimsLookup.ToDictionary(g => g.Key, g => (float)g.Average(c => c.Count));

            // OPTIMIZATION 2: Parallel feature extraction
            var claimFeaturesList = claims.AsParallel().Select(c => MapToClaimFeaturesOptimized(c, userClaimsLookup, receiptHashLookup, userAverages, includeLabel: true)).ToList();

            // Load into ML.NET
            var trainingData = _mlContext.Data.LoadFromEnumerable(claimFeaturesList);

            // OPTIMIZATION 3: Pre-defined feature columns (avoid reflection)
            var featureColumns = new[]
            {
                nameof(ClaimFeatures.Amount),
                nameof(ClaimFeatures.DaysSinceLastClaim),
                nameof(ClaimFeatures.SubmissionDelayDays),
                nameof(ClaimFeatures.VendorFrequency),
                nameof(ClaimFeatures.CategoryFrequency),
                nameof(ClaimFeatures.AverageClaimAmountForUser),
                nameof(ClaimFeatures.AmountDeviationFromAverage),
                nameof(ClaimFeatures.IPAddressChangeFrequency),
                nameof(ClaimFeatures.ItemCount),
                nameof(ClaimFeatures.DistinctItemsRatio),
                nameof(ClaimFeatures.ReceiptHashDuplicateCount),
                nameof(ClaimFeatures.ReceiptHashFrequencyForUser),
                nameof(ClaimFeatures.ItemValidationScore) // Include item validation score in training
            };

            // OPTIMIZATION 4: Optimized LightGBM parameters for faster training
            var pipeline = _mlContext.Transforms.Concatenate("Features", featureColumns)
                .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                    labelColumnName: nameof(ClaimFeatures.IsFraudulent),
                    featureColumnName: "Features",
                    numberOfIterations: 100,      // Reduced from default 100
                    numberOfLeaves: 20,           // Reduced from default 31
                    minimumExampleCountPerLeaf: 10, // Increased from default 1
                    learningRate: 0.1             // Slightly higher learning rate
                ));

            // Train model
            var model = pipeline.Fit(trainingData);

            // Save model to file
            var modelPath = Path.Combine(AppContext.BaseDirectory, "fraudModel.zip");
            _mlContext.Model.Save(model, trainingData.Schema, modelPath);

            return modelPath;
        }

        public async Task<string> TrainModelAsync()
        {
            return await Task.Run(() => TrainModel());
        }

        private ClaimFeatures MapToClaimFeaturesOptimized(
            Claim claim, 
            ILookup<string, List<Claim>> userClaimsLookup,
            ILookup<string, int> receiptHashLookup,
            Dictionary<string, float> userAverages,
            bool includeLabel = false)
        {
            var userClaims = userClaimsLookup[claim.UserId].FirstOrDefault() ?? new List<Claim>();
            var userAverage = userAverages.GetValueOrDefault(claim.UserId, 0f);

            var features = new ClaimFeatures
            {
                Amount = (float)claim.Amount,
                DaysSinceLastClaim = CalculateDaysSinceLastClaimOptimized(userClaims, claim.DateOfService),
                SubmissionDelayDays = (float)(claim.SubmissionDate - claim.DateOfService).TotalDays,
                VendorFrequency = CalculateVendorFrequencyOptimized(userClaims, claim.VendorId),
                CategoryFrequency = CalculateCategoryFrequencyOptimized(userClaims, claim.Category),
                AverageClaimAmountForUser = userAverage,
                AmountDeviationFromAverage = userAverage > 0 ? (float)Math.Abs(claim.Amount - userAverage) / userAverage : 0f,
                IPAddressChangeFrequency = CalculateIPChangeFrequencyOptimized(userClaims),
                ItemCount = claim.Items?.Count ?? 0,
                DistinctItemsRatio = CalculateDistinctItemRatio(claim.Items),
                ReceiptHashDuplicateCount = Math.Max(0f, receiptHashLookup[claim.ReceiptHash].FirstOrDefault() - 1),
                ReceiptHashFrequencyForUser = CalculateReceiptHashFrequencyOptimized(userClaims, claim.ReceiptHash),
                ItemValidationScore = claim.ItemValidationScore // Include item validation score
            };

            if (includeLabel)
            {
                features.IsFraudulent = claim.IsFraudulent == 1;
            }

            return features;
        }

        private float CalculateDaysSinceLastClaimOptimized(List<Claim> userClaims, DateTime claimDate)
        {
            var previousClaim = userClaims
                .Where(c => c.SubmissionDate < claimDate)
                .OrderByDescending(c => c.SubmissionDate)
                .FirstOrDefault();

            return previousClaim == null ? 9999f : (float)(claimDate - previousClaim.SubmissionDate).TotalDays;
        }

        private float CalculateVendorFrequencyOptimized(List<Claim> userClaims, string vendorId)
        {
            if (!userClaims.Any()) return 0f;
            var vendorCount = userClaims.Count(c => c.VendorId == vendorId);
            return (float)vendorCount / userClaims.Count;
        }

        private float CalculateCategoryFrequencyOptimized(List<Claim> userClaims, string category)
        {
            if (!userClaims.Any()) return 0f;
            var categoryCount = userClaims.Count(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
            return (float)categoryCount / userClaims.Count;
        }

        private float CalculateIPChangeFrequencyOptimized(List<Claim> userClaims)
        {
            var orderedClaims = userClaims.OrderBy(c => c.SubmissionDate).ToArray();
            if (orderedClaims.Length <= 1) return 0f;

            int changeCount = 0;
            for (int i = 1; i < orderedClaims.Length; i++)
            {
                if (!string.Equals(orderedClaims[i].IPAddress, orderedClaims[i-1].IPAddress, StringComparison.OrdinalIgnoreCase))
                    changeCount++;
            }

            return (float)changeCount / (orderedClaims.Length - 1);
        }

        private float CalculateReceiptHashFrequencyOptimized(List<Claim> userClaims, string receiptHash)
        {
            if (string.IsNullOrEmpty(receiptHash) || !userClaims.Any()) return 0f;
            var hashMatchCount = userClaims.Count(c => string.Equals(c.ReceiptHash, receiptHash, StringComparison.OrdinalIgnoreCase));
            return (float)hashMatchCount / userClaims.Count;
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
                ItemValidationScore = claim.ItemValidationScore // Include item validation score
            };

            if (includeLabel)
            {
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

            if (previousClaim == null) return 9999f;

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
    }
}

