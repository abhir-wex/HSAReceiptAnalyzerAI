using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services.Interface;
using HSAReceiptAnalyzer.Services;
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
        private readonly ISemanticKernelService _skService;
        private readonly ILogger<RAGAnalyzeController> _logger;

        public RAGAnalyzeController(
            IRAGService ragService,
            IFormRecognizerService formService,
            IClaimDatabaseManager claimManager,
            IFraudDetectionService fraudDetectionService,
            ISemanticKernelService skService,
            ILogger<RAGAnalyzeController> logger)
        {
            _ragService = ragService;
            _formService = formService;
            _claimManager = claimManager;
            _fraudDetectionService = fraudDetectionService;
            _skService = skService;
            _logger = logger;
        }

        [HttpPost("enhanced-fraud-check")]
        public async Task<IActionResult> EnhancedFraudCheck([FromForm] ImageUploadRequest request)
        {
            try
            {
                // Debug logging
                _logger.LogInformation("=== RAG ENHANCED FRAUD CHECK REQUEST ===");
                _logger.LogInformation("UserId: {UserId}", request.UserId);
                _logger.LogInformation("Date: {Date}", request.Date);
                _logger.LogInformation("Amount: {Amount}", request.Amount);
                _logger.LogInformation("Merchant: {Merchant}", request.Merchant);
                _logger.LogInformation("Description: {Description}", request.Description);
                _logger.LogInformation("Image: {FileName} ({Length} bytes)", request.Image?.FileName, request.Image?.Length);

                // Step 1: Extract receipt data using Form Recognizer
                var receiptData = await _formService.ExtractDataAsync(request);

                _logger.LogInformation("Generated Receipt Hash: {ReceiptHash}", receiptData.ReceiptHash);
                _logger.LogInformation("Receipt Amount: {Amount}", receiptData.Amount);
                _logger.LogInformation("Receipt Merchant: {Merchant}", receiptData.Merchant);
                _logger.LogInformation("Receipt Items: {Items}", string.Join(", ", receiptData.Items ?? new List<string>()));

                // Step 2: Check for duplicate receipts
                bool duplicateFound = _claimManager.ExistsDuplicate(receiptData.ReceiptHash, receiptData.UserId);
                _logger.LogInformation("Duplicate check result: {DuplicateFound}", duplicateFound);

                if (duplicateFound)
                {
                    _logger.LogWarning("DUPLICATE DETECTED - Returning fraud response");
                    
                    // Enhanced duplicate response with RAG context
                    var duplicateAnalysis = await _ragService.AnalyzeClaimWithRAGAsync(receiptData, "Analyze this duplicate receipt case for historical patterns");
                    
                    return Ok(new
                    {
                        ClaimId = receiptData.ClaimId,
                        IsFraudulent = true,
                        FraudScore = 95,
                        RiskLevel = "High",
                        Message = "? Duplicate receipt detected - This receipt has been submitted by another user.",
                        UserReadableText = "This receipt has already been submitted by a different user. Duplicate receipts across multiple users indicate potential fraudulent activity. Each receipt should only be submitted once by the original recipient of services.",
                        TechnicalDetails = $"Receipt hash '{receiptData.ReceiptHash}' already exists in the system under a different user ID. This indicates the same physical receipt is being reused for multiple HSA claims.",
                        FraudReason = "DuplicateReceipt",
                        RAGAnalysis = duplicateAnalysis.Analysis,
                        SimilarHistoricalCases = duplicateAnalysis.SimilarCases.Take(3).Select(c => new
                        {
                            Relevance = c.Relevance,
                            Summary = c.Content.Length > 200 ? c.Content.Substring(0, 200) + "..." : c.Content,
                            Source = c.Source
                        }),
                        RecommendedAction = "Immediate investigation required - cross-reference receipt usage",
                        ItemValidation = new
                        {
                            Score = receiptData.ItemValidationScore,
                            ValidItems = receiptData.ValidItems,
                            InvalidItems = receiptData.InvalidItems,
                            Notes = "Item validation not applicable - claim rejected due to duplicate receipt detection",
                            TotalItems = receiptData.Items?.Count ?? 0,
                            ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                            InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0
                        },
                        AnalysisTimestamp = DateTime.UtcNow
                    });
                }

                // Step 3: Check for invalid HSA items (enhanced fraud rule)
                bool invalidItemsFraud = false;
                string invalidItemsReason = "";
                List<string> suspiciousItems = new();
                
                if (receiptData.ItemValidationScore < 20)
                {
                    invalidItemsFraud = true;
                    invalidItemsReason = $"Extremely low item validation score ({receiptData.ItemValidationScore:F1}%) - mostly non-HSA eligible items";
                }
                else if (receiptData.InvalidItems?.Count > 0 && receiptData.Items?.Count > 0)
                {
                    var invalidRatio = (float)receiptData.InvalidItems.Count / receiptData.Items.Count;
                    if (invalidRatio >= 0.7f) // 70% or more invalid items
                    {
                        invalidItemsFraud = true;
                        invalidItemsReason = $"High ratio of invalid items ({invalidRatio:P0}) - {receiptData.InvalidItems.Count} invalid out of {receiptData.Items.Count} total";
                    }
                }

                // Check for known non-HSA items that indicate clear fraud
                var knownNonHsaItems = new[] { "alcohol", "beer", "wine", "cigarettes", "tobacco", "candy", "soda", "chips" };
                suspiciousItems = receiptData.InvalidItems?.Where(item => 
                    knownNonHsaItems.Any(prohibited => item.Contains(prohibited, StringComparison.OrdinalIgnoreCase))
                ).ToList() ?? new List<string>();

                if (suspiciousItems.Any())
                {
                    invalidItemsFraud = true;
                    invalidItemsReason = $"Contains clearly non-HSA eligible items: {string.Join(", ", suspiciousItems)}";
                }

                if (invalidItemsFraud)
                {
                    _logger.LogWarning("INVALID ITEMS DETECTED - {Reason}", invalidItemsReason);
                    
                    try
                    {
                        // Enhanced analysis for invalid items using both RAG and SK
                        var invalidItemsRAGAnalysis = await _ragService.AnalyzeClaimWithRAGAsync(receiptData, "Analyze this claim with invalid HSA items for historical patterns");
                        var invalidItemsAIAnalysis = await _skService.AnalyzeReceiptAsync(receiptData);
                        
                        return Ok(new
                        {
                            ClaimId = receiptData.ClaimId,
                            IsFraudulent = true,
                            FraudScore = 85,
                            RiskLevel = "High",
                            Message = $"? Invalid HSA items detected: {invalidItemsReason}",
                            UserReadableText = invalidItemsAIAnalysis ?? $"This receipt contains items that are not eligible for HSA reimbursement. {invalidItemsReason}",
                            TechnicalDetails = invalidItemsReason,
                            FraudReason = "InvalidHSAItems",
                            RAGAnalysis = invalidItemsRAGAnalysis.Analysis,
                            RAGConfidence = invalidItemsRAGAnalysis.ConfidenceScore,
                            SimilarHistoricalCases = invalidItemsRAGAnalysis.SimilarCases.Take(3).Select(c => new
                            {
                                Relevance = c.Relevance,
                                Summary = c.Content.Length > 200 ? c.Content.Substring(0, 200) + "..." : c.Content,
                                Source = c.Source
                            }),
                            RiskFactors = invalidItemsRAGAnalysis.RiskFactors,
                            RecommendedAction = invalidItemsRAGAnalysis.RecommendedAction,
                            ItemValidation = new
                            {
                                Score = receiptData.ItemValidationScore,
                                ValidItems = receiptData.ValidItems,
                                InvalidItems = receiptData.InvalidItems,
                                Notes = receiptData.ItemValidationNotes,
                                TotalItems = receiptData.Items?.Count ?? 0,
                                ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                                InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0,
                                SuspiciousItems = suspiciousItems,
                                IsItemValidationFraud = invalidItemsFraud,
                                InvalidItemsRatio = receiptData.Items?.Count > 0 ? (float)receiptData.InvalidItems?.Count / receiptData.Items.Count : 0f
                            },
                            AnalysisTimestamp = invalidItemsRAGAnalysis.AnalysisTimestamp
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in AI analysis for invalid items");
                        return Ok(new
                        {
                            ClaimId = receiptData.ClaimId,
                            IsFraudulent = true,
                            FraudScore = 85,
                            RiskLevel = "High",
                            Message = $"? Invalid HSA items detected: {invalidItemsReason}",
                            UserReadableText = $"This receipt contains items that are not eligible for HSA reimbursement. {invalidItemsReason}",
                            TechnicalDetails = invalidItemsReason,
                            FraudReason = "InvalidHSAItems",
                            RAGAnalysis = "AI analysis temporarily unavailable - fraud detection based on item validation rules",
                            ItemValidation = new
                            {
                                Score = receiptData.ItemValidationScore,
                                ValidItems = receiptData.ValidItems,
                                InvalidItems = receiptData.InvalidItems,
                                Notes = receiptData.ItemValidationNotes,
                                TotalItems = receiptData.Items?.Count ?? 0,
                                ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                                InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0,
                                SuspiciousItems = suspiciousItems,
                                IsItemValidationFraud = invalidItemsFraud
                            },
                            AnalysisTimestamp = DateTime.UtcNow
                        });
                    }
                }

                // Step 4: Traditional ML fraud detection with AI analysis
                var mlPrediction = await _fraudDetectionService.PredictWithAIAnalysisAsync(receiptData);
                float mlFraudScore = Math.Clamp(mlPrediction.FinalFraudScore, 0f, 100f);

                // Step 5: RAG-enhanced analysis
                var ragAnalysis = await _ragService.AnalyzeClaimWithRAGAsync(receiptData);

                // Step 6: Combine ML and RAG insights
                var combinedScore = CalculateCombinedFraudScore(mlFraudScore, ragAnalysis);
                var riskLevel = DetermineRiskLevel(combinedScore);
                var isFraudulent = combinedScore >= 70;

                // Step 7: Determine fraud reason for flagged cases
                string fraudReason = "";
                if (isFraudulent)
                {
                    if (mlPrediction.MlScore >= 70)
                        fraudReason = "MachineLearningDetection";
                    else if (mlPrediction.RuleScore >= 70)
                        fraudReason = "RuleBasedDetection";
                    else if (mlPrediction.ItemValidationScore < 30)
                        fraudReason = "LowItemValidationScore";
                    else if (ragAnalysis.ConfidenceScore > 0.8)
                        fraudReason = "RAGPatternMatch";
                    else
                        fraudReason = "CombinedFactors";
                }

                // Step 8: Save claim to database
                _logger.LogInformation("Before inserting claim - Hash: {ReceiptHash}", receiptData.ReceiptHash);
                _claimManager.InsertClaim(receiptData);
                _logger.LogInformation("After inserting claim");

                // Step 9: If fraud detected, index it for future RAG analysis
                if (isFraudulent)
                {
                    receiptData.IsFraudulent = 1;
                    receiptData.FraudTemplate = DetermineFraudTemplate(ragAnalysis);
                    await _ragService.IndexFraudCaseAsync(receiptData);
                }

                _logger.LogInformation("Final RAG result: IsFraudulent={IsFraudulent}, Score={Score}", isFraudulent, combinedScore);

                return Ok(new
                {
                    ClaimId = receiptData.ClaimId,
                    IsFraudulent = isFraudulent,
                    FraudScore = combinedScore,
                    RiskLevel = riskLevel,
                    MLScore = mlFraudScore,
                    RAGConfidence = ragAnalysis.ConfidenceScore,
                    Message = GenerateEnhancedMessage(isFraudulent, combinedScore, riskLevel, ragAnalysis),
                    UserReadableText = mlPrediction.AIAnalysis ?? "AI analysis not available",
                    TechnicalDetails = mlPrediction.Explanation,
                    FraudReason = fraudReason,
                    RAGAnalysis = ragAnalysis.Analysis,
                    SimilarHistoricalCases = ragAnalysis.SimilarCases.Take(3).Select(c => new
                    {
                        Relevance = c.Relevance,
                        Summary = c.Content.Length > 200 ? c.Content.Substring(0, 200) + "..." : c.Content,
                        Source = c.Source
                    }),
                    RiskFactors = ragAnalysis.RiskFactors,
                    RecommendedAction = ragAnalysis.RecommendedAction,
                    
                    // Enhanced item validation information
                    ItemValidation = new
                    {
                        Score = mlPrediction.ItemValidationScore,
                        ValidItems = receiptData.ValidItems,
                        InvalidItems = receiptData.InvalidItems,
                        Notes = mlPrediction.ItemValidationDetails,
                        TotalItems = receiptData.Items?.Count ?? 0,
                        ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                        InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0,
                        InvalidItemsRatio = receiptData.Items?.Count > 0 ? (float)receiptData.InvalidItems?.Count / receiptData.Items.Count : 0f,
                        SuspiciousItems = suspiciousItems,
                        IsItemValidationFraud = invalidItemsFraud
                    },
                    AnalysisTimestamp = ragAnalysis.AnalysisTimestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced fraud check");
                return StatusCode(500, new { Error = "Internal server error during fraud analysis", Details = ex.Message });
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

                _logger.LogInformation("Admin analysis request: {Prompt}", request.Prompt);

                // Enhanced: Try RAG-based contextual analysis first (primary approach)
                try
                {
                    var allClaims = _claimManager.GetAllClaims();
                    var ragAnalysis = await _ragService.GetContextualAnalysisAsync(request.Prompt, allClaims);
                    
                    // Also search for specific similar patterns
                    var similarPatterns = await _ragService.FindSimilarFraudPatternsAsync();

                    return Ok(new
                    {
                        Query = request.Prompt,
                        SummaryType = "RAG_Enhanced_Analysis",
                        ContextualAnalysis = ragAnalysis,
                        AnalysisMethod = "RAG_Contextual",
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
                catch (Exception ragEx)
                {
                    _logger.LogWarning(ragEx, "RAG analysis failed, falling back to pattern analysis: {Message}", ragEx.Message);
                    
                    // Fallback to original pattern-based analysis if RAG fails
                    string route = await _skService.RouteAdminPromptAsync(request.Prompt);
                    var allClaimsForFallback = _claimManager.GetAllClaims();

                    object result = route switch
                    {
                        "SharedReceiptSummary" => new
                        {
                            SummaryType = "SharedFraudReceiptSummary",
                            Summary = _skService.SummarizeSharedReceiptFraud(allClaimsForFallback, request.Prompt)
                        },
                        "TemplateSummary" => new
                        {
                            SummaryType = "TemplateSummary",
                            Summary = SemanticKernelService.SummarizeByFraudTemplate(allClaimsForFallback, request.Prompt)
                        },
                        "UserAnomalySummary" => new
                        {
                            SummaryType = "UserAnomalySummary",
                            Summary = SemanticKernelService.SummarizeUserAnomalies(allClaimsForFallback, request.Prompt)
                        },
                        "HighRiskVendors" => new
                        {
                            SummaryType = "HighRiskVendors",
                            Summary = SemanticKernelService.SummarizeHighRiskVendors(allClaimsForFallback, request.Prompt)
                        },
                        "ClaimPatternClassifier" => new
                        {
                            SummaryType = "ClaimPatternClassifier",
                            Summary = _skService.SummarizeClaimPatterns(allClaimsForFallback, request.Prompt)
                        },
                        "ClaimTimeSpikeSummary" => new
                        {
                            SummaryType = "ClaimTimeSpikeSummary",
                            Summary = SemanticKernelService.SummarizeClaimTimeSpikes(allClaimsForFallback, request.Prompt)
                        },
                        "SuspiciousUserNetwork" => new
                        {
                            SummaryType = "SuspiciousUserNetwork",
                            Summary = SemanticKernelService.SummarizeSuspiciousUserLinks(allClaimsForFallback, request.Prompt)
                        },
                        "SuspiciousPatternAnalysis" => new
                        {
                            SummaryType = "SuspiciousPatternAnalysis",
                            Summary = await _skService.DetectSuspiciousPatterns(allClaimsForFallback, request.Prompt)
                        },
                        "RoundAmountPattern" => new
                        {
                            SummaryType = "RoundAmountPattern",
                            Summary = await _skService.DetectRoundAmountPatterns(allClaimsForFallback, request.Prompt)
                        },
                        "HighFrequencySubmissions" => new
                        {
                            SummaryType = "HighFrequencySubmissions",
                            Summary = await _skService.DetectHighFrequencySubmissions(allClaimsForFallback, request.Prompt)
                        },
                        "UnusualTimingPatterns" => new
                        {
                            SummaryType = "UnusualTimingPatterns",
                            Summary = await _skService.DetectUnusualTimingPatterns(allClaimsForFallback, request.Prompt)
                        },
                        "RapidSuccessionClaims" => new
                        {
                            SummaryType = "RapidSuccessionClaims",
                            Summary = await _skService.DetectRapidSuccessionClaims(allClaimsForFallback, request.Prompt)
                        },
                        "IPAnomalies" => new
                        {
                            SummaryType = "IPAnomalies",
                            Summary = await _skService.DetectIPAnomalies(allClaimsForFallback, request.Prompt)
                        },
                        "EscalatingAmounts" => new
                        {
                            SummaryType = "EscalatingAmounts",
                            Summary = await _skService.DetectEscalatingAmounts(allClaimsForFallback, request.Prompt)
                        },
                        _ => new
                        {
                            SummaryType = "Fallback_Analysis",
                            Summary = $"Could not classify admin prompt. Route: {route}. RAG analysis failed with: {ragEx.Message}"
                        }
                    };

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in contextual admin analysis: {Prompt}", request.Prompt);
                return StatusCode(500, new { Error = "Internal server error during analysis", Details = ex.Message });
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