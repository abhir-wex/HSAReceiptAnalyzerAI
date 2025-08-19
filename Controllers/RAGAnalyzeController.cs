using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace HSAReceiptAnalyzer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RAGAnalyzeController : ControllerBase
    {
        private readonly IRAGService _ragService;
        private readonly IFormRecognizerService _formService;
        private readonly IClaimDatabaseManager _claimManager;
        private readonly IFraudDetectionService _fraudDetectionService;
        private readonly ILogger<RAGAnalyzeController> _logger;

        public RAGAnalyzeController(
            IRAGService ragService,
            IFormRecognizerService formService,
            IClaimDatabaseManager claimManager,
            IFraudDetectionService fraudDetectionService,
            ILogger<RAGAnalyzeController> logger)
        {
            _ragService = ragService;
            _formService = formService;
            _claimManager = claimManager;
            _fraudDetectionService = fraudDetectionService;
            _logger = logger;
        }

        [HttpPost("enhanced-fraud-check")]
        public async Task<IActionResult> EnhancedFraudCheck([FromForm] ImageUploadRequest request)
        {
            try
            {
                // Step 1: Extract receipt data using Form Recognizer
                var receiptData = await _formService.ExtractDataAsync(request.Image);

                // Step 2: Check for duplicate receipts
                bool duplicateFound = _claimManager.ExistsDuplicate(receiptData.ReceiptHash, receiptData.UserId);

                if (duplicateFound)
                {
                    return Ok(new
                    {
                        ClaimId = receiptData.ClaimId,
                        IsFraudulent = true,
                        FraudScore = 95,
                        RiskLevel = "High",
                        Message = "? Receipt hash found in another user's claim.",
                        RAGAnalysis = "Duplicate receipt detected - automatic fraud classification",
                        SimilarCases = new List<object>(),
                        RecommendedAction = "Immediate investigation required - cross-reference receipt usage"
                    });
                }

                // Step 3: Traditional ML fraud detection
                var mlPrediction = _fraudDetectionService.Predict(receiptData);
                float mlFraudScore = Math.Clamp(mlPrediction.FinalFraudScore, 0f, 100f);

                // Step 4: RAG-enhanced analysis
                var ragAnalysis = await _ragService.AnalyzeClaimWithRAGAsync(receiptData);

                // Step 5: Combine ML and RAG insights
                var combinedScore = CalculateCombinedFraudScore(mlFraudScore, ragAnalysis);
                var riskLevel = DetermineRiskLevel(combinedScore);
                var isFraudulent = combinedScore >= 70;

                // Step 6: Save claim to database
                _claimManager.InsertClaim(receiptData);

                // Step 7: If fraud detected, index it for future RAG analysis
                if (isFraudulent)
                {
                    receiptData.IsFraudulent = 1;
                    receiptData.FraudTemplate = DetermineFraudTemplate(ragAnalysis);
                    await _ragService.IndexFraudCaseAsync(receiptData);
                }

                return Ok(new
                {
                    ClaimId = receiptData.ClaimId,
                    IsFraudulent = isFraudulent,
                    FraudScore = combinedScore,
                    RiskLevel = riskLevel,
                    MLScore = mlFraudScore,
                    RAGConfidence = ragAnalysis.ConfidenceScore,
                    Message = GenerateEnhancedMessage(isFraudulent, combinedScore, riskLevel, ragAnalysis),
                    RAGAnalysis = ragAnalysis.Analysis,
                    SimilarHistoricalCases = ragAnalysis.SimilarCases.Take(3).Select(c => new
                    {
                        Relevance = c.Relevance,
                        Summary = c.Content.Length > 200 ? c.Content.Substring(0, 200) + "..." : c.Content,
                        Source = c.Source
                    }),
                    RiskFactors = ragAnalysis.RiskFactors,
                    RecommendedAction = ragAnalysis.RecommendedAction,
                    AnalysisTimestamp = ragAnalysis.AnalysisTimestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced fraud check");
                return StatusCode(500, new { Error = "Internal server error during fraud analysis" });
            }
        }

        [HttpPost("contextual-admin-analysis")]
        public async Task<IActionResult> ContextualAdminAnalysis([FromBody] AdminPromptRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Prompt))
                {
                    return BadRequest("Prompt is required.");
                }

                // Get all claims for context
                var allClaims = _claimManager.GetAllClaims();

                // Use RAG for contextual analysis
                var ragAnalysis = await _ragService.GetContextualAnalysisAsync(request.Prompt, allClaims);

                // Also search for specific similar patterns
                var similarPatterns = await _ragService.FindSimilarFraudPatternsAsync();

                return Ok(new
                {
                    Query = request.Prompt,
                    ContextualAnalysis = ragAnalysis,
                    SimilarPatterns = similarPatterns.Take(5).Select(p => new
                    {
                        Relevance = p.Relevance,
                        Pattern = p.Content.Length > 300 ? p.Content.Substring(0, 300) + "..." : p.Content,
                        Metadata = p.Metadata
                    }),
                    TotalClaimsAnalyzed = allClaims.Count,
                    FraudCasesInKnowledgeBase = allClaims.Count(c => c.IsFraudulent == 1),
                    AnalysisTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in contextual admin analysis: {Prompt}", request.Prompt);
                return StatusCode(500, new { Error = "Internal server error during analysis" });
            }
        }

        [HttpPost("fraud-trends-analysis")]
        public async Task<IActionResult> FraudTrendsAnalysis([FromBody] AdminPromptRequest request)
        {
            try
            {
                var query = request?.Prompt ?? "fraud trends and patterns analysis";
                
                var trendsAnalysis = await _ragService.GetFraudTrendsInsightsAsync(query);
                var allClaims = _claimManager.GetAllClaims();
                
                // Get recent fraud statistics
                var recentFraudStats = GetRecentFraudStatistics(allClaims);

                return Ok(new
                {
                    Query = query,
                    TrendsAnalysis = trendsAnalysis,
                    RecentStatistics = recentFraudStats,
                    AnalysisTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fraud trends analysis");
                return StatusCode(500, new { Error = "Internal server error during trends analysis" });
            }
        }

        [HttpPost("search-similar-cases")]
        public async Task<IActionResult> SearchSimilarCases([FromBody] SimilarCaseRequest request)
        {
            try
            {
                var similarCases = await _ragService.FindSimilarFraudPatternsAsync(
                    merchant: request.Merchant,
                    amount: request.Amount,
                    serviceType: request.ServiceType,
                    fraudTemplate: request.FraudTemplate
                );

                return Ok(new
                {
                    SearchCriteria = request,
                    SimilarCases = similarCases.Select(c => new
                    {
                        Relevance = c.Relevance,
                        Content = c.Content,
                        Metadata = c.Metadata,
                        Source = c.Source
                    }),
                    TotalFound = similarCases.Count,
                    SearchTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching similar cases");
                return StatusCode(500, new { Error = "Internal server error during case search" });
            }
        }

        [HttpPost("rebuild-knowledge-base")]
        public async Task<IActionResult> RebuildKnowledgeBase()
        {
            try
            {
                await _ragService.RebuildKnowledgeBaseAsync();
                
                return Ok(new
                {
                    Message = "RAG Knowledge Base rebuilt successfully",
                    RebuildTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding knowledge base");
                return StatusCode(500, new { Error = "Internal server error during knowledge base rebuild" });
            }
        }

        #region Private Helper Methods

        private float CalculateCombinedFraudScore(float mlScore, RAGAnalysisResult ragAnalysis)
        {
            // Weighted combination of ML score and RAG confidence
            var ragScore = (float)(ragAnalysis.ConfidenceScore * 100);
            var similarCasesWeight = ragAnalysis.SimilarCases.Count > 0 ? 10f : 0f;
            var riskFactorWeight = ragAnalysis.RiskFactors.Count * 5f;

            var combinedScore = (mlScore * 0.6f) + (ragScore * 0.3f) + similarCasesWeight + riskFactorWeight;
            
            return Math.Clamp(combinedScore, 0f, 100f);
        }

        private string DetermineRiskLevel(float score)
        {
            return score switch
            {
                < 40 => "Low",
                < 70 => "Medium",
                _ => "High"
            };
        }

        private string DetermineFraudTemplate(RAGAnalysisResult ragAnalysis)
        {
            if (ragAnalysis.RiskFactors.Any(rf => rf.Contains("receipt")))
                return "SharedReceiptAcrossUsers";
            
            if (ragAnalysis.RiskFactors.Any(rf => rf.Contains("round amount")))
                return "RoundAmountPattern";
            
            if (ragAnalysis.RiskFactors.Any(rf => rf.Contains("same-day")))
                return "HighFrequencySubmission";

            return "SuspiciousPattern";
        }

        private string GenerateEnhancedMessage(bool isFraudulent, float score, string riskLevel, RAGAnalysisResult ragAnalysis)
        {
            if (isFraudulent)
            {
                var similarCasesCount = ragAnalysis.SimilarCases.Count;
                var historicalContext = similarCasesCount > 0 
                    ? $" This pattern matches {similarCasesCount} historical fraud case(s)."
                    : " This represents a new fraud pattern.";

                return $"? FRAUD DETECTED ({score:F1}%, {riskLevel} risk).{historicalContext} Key factors: {string.Join(", ", ragAnalysis.RiskFactors)}.";
            }
            else
            {
                return $"? Claim appears legitimate ({score:F1}%, {riskLevel} risk). No significant fraud indicators detected.";
            }
        }

        private object GetRecentFraudStatistics(List<Claim> allClaims)
        {
            var recentClaims = allClaims.Where(c => c.SubmissionDate >= DateTime.Now.AddDays(-30)).ToList();
            var fraudClaims = recentClaims.Where(c => c.IsFraudulent == 1).ToList();

            return new
            {
                TotalClaimsLast30Days = recentClaims.Count,
                FraudClaimsLast30Days = fraudClaims.Count,
                FraudRate = recentClaims.Count > 0 ? (double)fraudClaims.Count / recentClaims.Count * 100 : 0,
                TopFraudMerchants = fraudClaims
                    .GroupBy(c => c.Merchant)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new { Merchant = g.Key, Count = g.Count() }),
                TopFraudTemplates = fraudClaims
                    .GroupBy(c => c.FraudTemplate)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new { Template = g.Key, Count = g.Count() })
            };
        }

        #endregion
    }

    public class SimilarCaseRequest
    {
        public string? Merchant { get; set; }
        public double? Amount { get; set; }
        public string? ServiceType { get; set; }
        public string? FraudTemplate { get; set; }
    }
}