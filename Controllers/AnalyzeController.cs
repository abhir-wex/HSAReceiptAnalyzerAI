using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HSAReceiptAnalyzer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalyzeController : ControllerBase
    {
        private readonly ISemanticKernelService _skService;
        private readonly IFormRecognizerService _formService;
        private readonly IClaimDatabaseManager _claimDatabaseManager;
        private readonly IFraudDetectionService _fraudDetectionService;
        private readonly IRAGService _ragService;

        public AnalyzeController(
            ISemanticKernelService skService, 
            IFormRecognizerService formService, 
            IClaimDatabaseManager claimDatabaseManager,
            IFraudDetectionService fraudDetectionService,
            IRAGService ragService)
        {
            _skService = skService;
            _formService = formService;
            _claimDatabaseManager = claimDatabaseManager;
            _fraudDetectionService = fraudDetectionService;
            _ragService = ragService;
        }

        [HttpPost("fraud-check")]
        public async Task<IActionResult> AnalyzeClaim([FromForm] ImageUploadRequest request)
        {
            var receiptData = await _formService.ExtractDataAsync(request.Image);

            bool duplicateFound = _claimDatabaseManager.ExistsDuplicate(receiptData.ReceiptHash, receiptData.UserId);

            if (duplicateFound)
            {
                return Ok(new
                {
                    ClaimId = receiptData.ClaimId,
                    IsFraudulent = true,
                    FraudScore = 95,
                    Message = "⚠ Receipt hash found in another user's claim."
                });
            }

            // --- Step 2: ML prediction (supervised LightGBM) ---
            var prediction = _fraudDetectionService.Predict(receiptData);

            
            float fraudLikelihood = Math.Clamp(prediction.FinalFraudScore, 0f, 100f);

            // --- Step 3: Assign risk levels based on fraud score ---
            string riskLevel = fraudLikelihood switch
            {
                < 40 => "Low",
                < 70 => "Medium",
                _ => "High"
            };


            // --- Step 4: Determine if fraudulent based on consistent thresholds ---
            bool isFraudulent = fraudLikelihood >= 70; // High risk threshold

            _claimDatabaseManager.InsertClaim(receiptData);
            // --- Step 5: Human-readable message based on consistent logic ---
            string message = isFraudulent
                ? $"⚠ This claim is potentially fraudulent ({fraudLikelihood:F2}%, {riskLevel} risk)."
                : $"✅ This claim appears normal ({fraudLikelihood:F2}%, {riskLevel} risk).";

            // --- Step 6: Response ---
            return Ok(new
            {
                ClaimId = receiptData.ClaimId,
                IsFraudulent = isFraudulent,
                FraudScore = fraudLikelihood,
                RiskLevel = riskLevel,
                Message = message
            });
        }

        [HttpPost("adminAnalyze")]
        public async Task<IActionResult> AnalyzeClaims([FromBody] AdminPromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
            {
                return BadRequest("Prompt is required.");
            }

            // Enhanced: Try RAG-based contextual analysis first
            try
            {
                var allClaims = _claimDatabaseManager.GetAllClaims();
                var ragAnalysis = await _ragService.GetContextualAnalysisAsync(request.Prompt, allClaims);
                
                return Ok(new
                {
                    SummaryType = "RAG_Enhanced_Analysis",
                    Summary = ragAnalysis,
                    AnalysisMethod = "RAG_Contextual",
                    TotalClaims = allClaims.Count,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Fallback to original pattern-based analysis if RAG fails
                Console.WriteLine($"RAG analysis failed, falling back to pattern analysis: {ex.Message}");
            }

            // Original pattern-based analysis as fallback
            string route = await _skService.RouteAdminPromptAsync(request.Prompt);
            var allClaimsForFallback = _claimDatabaseManager.GetAllClaims();

            object result;

            switch (route)
            {
                case "SharedReceiptSummary":
                    var summary = _skService.SummarizeSharedReceiptFraud(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "SharedFraudReceiptSummary",
                        Summary = summary
                    };
                    break;
                case "TemplateSummary":
                    result = new
                    {
                        SummaryType = "TemplateSummary",
                        Summary = SemanticKernelService.SummarizeByFraudTemplate(allClaimsForFallback, request.Prompt)
                    };
                    break;
                case "UserAnomalySummary":
                    result = new
                    {
                        SummaryType = "UserAnomalySummary",
                        Summary = SemanticKernelService.SummarizeUserAnomalies(allClaimsForFallback, request.Prompt)
                    };
                    break;
                case "HighRiskVendors":
                    result = new
                    {
                        SummaryType = "HighRiskVendors",
                        Summary = SemanticKernelService.SummarizeHighRiskVendors(allClaimsForFallback, request.Prompt)
                    };
                    break;

                case "ClaimPatternClassifier":
                    var pattern = _skService.SummarizeClaimPatterns(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "ClaimPatternClassifier",
                        Summary = pattern
                    };
                    break;
                case "ClaimTimeSpikeSummary":
                    result = new
                    {
                        SummaryType = "ClaimTimeSpikeSummary",
                        Summary = SemanticKernelService.SummarizeClaimTimeSpikes(allClaimsForFallback, request.Prompt)
                    };
                    break;

                case "SuspiciousUserNetwork":
                    result = new
                    {
                        SummaryType = "SuspiciousUserNetwork",
                        Summary = SemanticKernelService.SummarizeSuspiciousUserLinks(allClaimsForFallback, request.Prompt)
                    };
                    break;

                case "SuspiciousPatternAnalysis":
                    var suspiciousPatterns = await _skService.DetectSuspiciousPatterns(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "SuspiciousPatternAnalysis",
                        Summary = suspiciousPatterns
                    };
                    break;

                case "RoundAmountPattern":
                    var roundAmountAnalysis = await _skService.DetectRoundAmountPatterns(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "RoundAmountPattern",
                        Summary = roundAmountAnalysis
                    };
                    break;

                case "HighFrequencySubmissions":
                    var highFrequencyAnalysis = await _skService.DetectHighFrequencySubmissions(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "HighFrequencySubmissions",
                        Summary = highFrequencyAnalysis
                    };
                    break;

                case "UnusualTimingPatterns":
                    var timingPatterns = await _skService.DetectUnusualTimingPatterns(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "UnusualTimingPatterns",
                        Summary = timingPatterns
                    };
                    break;

                case "RapidSuccessionClaims":
                    var rapidSuccession = await _skService.DetectRapidSuccessionClaims(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "RapidSuccessionClaims",
                        Summary = rapidSuccession
                    };
                    break;

                case "IPAnomalies":
                    var ipAnomalies = await _skService.DetectIPAnomalies(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "IPAnomalies",
                        Summary = ipAnomalies
                    };
                    break;

                case "EscalatingAmounts":
                    var escalatingAmounts = await _skService.DetectEscalatingAmounts(allClaimsForFallback, request.Prompt);
                    result = new
                    {
                        SummaryType = "EscalatingAmounts",
                        Summary = escalatingAmounts
                    };
                    break;

                default:
                    return BadRequest($"Could not classify admin prompt. Got: {route}");
            }

            return Ok(result);
        }
    }
}

