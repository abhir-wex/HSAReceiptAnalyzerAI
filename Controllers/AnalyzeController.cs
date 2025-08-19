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

        public AnalyzeController(ISemanticKernelService skService, IFormRecognizerService formService, IClaimDatabaseManager claimDatabaseManager
, IFraudDetectionService fraudDetectionService)
        {
            _skService = skService;
            _formService = formService;
            _claimDatabaseManager = claimDatabaseManager;
            _fraudDetectionService = fraudDetectionService;
        }

        [HttpPost("fraud-check")]
        public async Task<IActionResult> AnalyzeClaim([FromForm] ImageUploadRequest request)
        {
            // Debug logging
            Console.WriteLine($"=== FRAUD CHECK REQUEST ===");
            Console.WriteLine($"UserId: {request.UserId}");
            Console.WriteLine($"Date: {request.Date}");
            Console.WriteLine($"Amount: {request.Amount}");
            Console.WriteLine($"Merchant: {request.Merchant}");
            Console.WriteLine($"Description: {request.Description}");
            Console.WriteLine($"Image: {request.Image?.FileName} ({request.Image?.Length} bytes)");
            
            var receiptData = await _formService.ExtractDataAsync(request);
            
            Console.WriteLine($"Generated Receipt Hash: {receiptData.ReceiptHash}");
            Console.WriteLine($"Receipt Amount: {receiptData.Amount}");
            Console.WriteLine($"Receipt Merchant: {receiptData.Merchant}");
            Console.WriteLine($"Receipt Items: {string.Join(", ", receiptData.Items ?? new List<string>())}");

            // --- Step 1: Check for duplicate receipts (existing fraud rule) ---
            bool duplicateFound = _claimDatabaseManager.ExistsDuplicate(receiptData.ReceiptHash, receiptData.UserId);
            Console.WriteLine($"Duplicate check result: {duplicateFound}");

            if (duplicateFound)
            {
                Console.WriteLine("DUPLICATE DETECTED - Returning fraud response");
                
                // For duplicate receipts, provide specific fraud reasoning focused on the duplicate nature
                // Don't generate AI analysis that might confuse with item validation
                return Ok(new
                {
                    ClaimId = receiptData.ClaimId,
                    IsFraudulent = true,
                    FraudScore = 95,
                    RiskLevel = "High",
                    Message = "⚠ Duplicate receipt detected - This receipt has been submitted by another user.",
                    UserReadableText = "This receipt has already been submitted by a different user. Duplicate receipts across multiple users indicate potential fraudulent activity. Each receipt should only be submitted once by the original recipient of services.",
                    TechnicalDetails = $"Receipt hash '{receiptData.ReceiptHash}' already exists in the system under a different user ID. This indicates the same physical receipt is being reused for multiple HSA claims.",
                    FraudReason = "DuplicateReceipt",
                    ItemValidation = new
                    {
                        Score = receiptData.ItemValidationScore,
                        ValidItems = receiptData.ValidItems,
                        InvalidItems = receiptData.InvalidItems,
                        Notes = "Item validation not applicable - claim rejected due to duplicate receipt detection",
                        TotalItems = receiptData.Items?.Count ?? 0,
                        ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                        InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0
                    }
                });
            }

            // --- Step 2: Check for invalid HSA items (new fraud rule) ---
            // If more than 70% of items are invalid, or validation score is very low, flag as fraud
            bool invalidItemsFraud = false;
            string invalidItemsReason = "";
            
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
            var suspiciousItems = receiptData.InvalidItems?.Where(item => 
                knownNonHsaItems.Any(prohibited => item.Contains(prohibited, StringComparison.OrdinalIgnoreCase))
            ).ToList() ?? new List<string>();

            if (suspiciousItems.Any())
            {
                invalidItemsFraud = true;
                invalidItemsReason = $"Contains clearly non-HSA eligible items: {string.Join(", ", suspiciousItems)}";
            }

            if (invalidItemsFraud)
            {
                Console.WriteLine($"INVALID ITEMS DETECTED - {invalidItemsReason}");
                try
                {
                    var invalidItemsAIAnalysis = await _skService.AnalyzeReceiptAsync(receiptData);
                    
                    return Ok(new
                    {
                        ClaimId = receiptData.ClaimId,
                        IsFraudulent = true,
                        FraudScore = 85,
                        RiskLevel = "High",
                        Message = $"⚠ Invalid HSA items detected: {invalidItemsReason}",
                        UserReadableText = invalidItemsAIAnalysis ?? $"This receipt contains items that are not eligible for HSA reimbursement. {invalidItemsReason}",
                        TechnicalDetails = invalidItemsReason,
                        FraudReason = "InvalidHSAItems",
                        ItemValidation = new
                        {
                            Score = receiptData.ItemValidationScore,
                            ValidItems = receiptData.ValidItems,
                            InvalidItems = receiptData.InvalidItems,
                            Notes = receiptData.ItemValidationNotes,
                            TotalItems = receiptData.Items?.Count ?? 0,
                            ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                            InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0,
                            SuspiciousItems = suspiciousItems
                        }
                    });
                }
                catch (Exception ex)
                {
                    return Ok(new
                    {
                        ClaimId = receiptData.ClaimId,
                        IsFraudulent = true,
                        FraudScore = 85,
                        RiskLevel = "High",
                        Message = $"⚠ Invalid HSA items detected: {invalidItemsReason}",
                        UserReadableText = $"This receipt contains items that are not eligible for HSA reimbursement. {invalidItemsReason}",
                        TechnicalDetails = invalidItemsReason,
                        FraudReason = "InvalidHSAItems",
                        ItemValidation = new
                        {
                            Score = receiptData.ItemValidationScore,
                            ValidItems = receiptData.ValidItems,
                            InvalidItems = receiptData.InvalidItems,
                            Notes = receiptData.ItemValidationNotes,
                            TotalItems = receiptData.Items?.Count ?? 0,
                            ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                            InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0,
                            SuspiciousItems = suspiciousItems
                        }
                    });
                }
            }

            // --- Step 3: ML prediction with AI analysis (enhanced) ---
            var prediction = await _fraudDetectionService.PredictWithAIAnalysisAsync(receiptData);

            float fraudLikelihood = Math.Clamp(prediction.FinalFraudScore, 0f, 100f);

            // --- Step 4: Assign risk levels based on fraud score ---
            string riskLevel = fraudLikelihood switch
            {
                < 40 => "Low",
                < 70 => "Medium",
                _ => "High"
            };

            // --- Step 5: Determine if fraudulent based on consistent thresholds ---
            bool isFraudulent = fraudLikelihood >= 70; // High risk threshold

            // --- Step 6: Additional fraud reason determination ---
            string fraudReason = "";
            if (isFraudulent)
            {
                if (prediction.MlScore >= 70)
                    fraudReason = "MachineLearningDetection";
                else if (prediction.RuleScore >= 70)
                    fraudReason = "RuleBasedDetection";
                else if (prediction.ItemValidationScore < 30)
                    fraudReason = "LowItemValidationScore";
                else
                    fraudReason = "CombinedFactors";
            }

            Console.WriteLine($"Before inserting claim - Hash: {receiptData.ReceiptHash}");
            _claimDatabaseManager.InsertClaim(receiptData);
            Console.WriteLine($"After inserting claim");
            
            // --- Step 7: Human-readable message based on consistent logic ---
            string message = isFraudulent
                ? $"⚠ This claim is potentially fraudulent ({fraudLikelihood:F2}%, {riskLevel} risk)."
                : $"✅ This claim appears normal ({fraudLikelihood:F2}%, {riskLevel} risk).";

            Console.WriteLine($"Final result: IsFraudulent={isFraudulent}, Score={fraudLikelihood}");

            // --- Step 8: Enhanced Response with AI Analysis and Item Validation ---
            return Ok(new
            {
                ClaimId = receiptData.ClaimId,
                IsFraudulent = isFraudulent,
                FraudScore = fraudLikelihood,
                RiskLevel = riskLevel,
                Message = message,
                UserReadableText = prediction.AIAnalysis ?? "AI analysis not available",
                TechnicalDetails = prediction.Explanation,
                MlScore = prediction.MlScore,
                RuleScore = prediction.RuleScore,
                FraudReason = fraudReason,
                
                // Enhanced item validation information
                ItemValidation = new
                {
                    Score = prediction.ItemValidationScore,
                    ValidItems = receiptData.ValidItems,
                    InvalidItems = receiptData.InvalidItems,
                    Notes = prediction.ItemValidationDetails,
                    TotalItems = receiptData.Items?.Count ?? 0,
                    ValidItemsCount = receiptData.ValidItems?.Count ?? 0,
                    InvalidItemsCount = receiptData.InvalidItems?.Count ?? 0,
                    InvalidItemsRatio = receiptData.Items?.Count > 0 ? (float)receiptData.InvalidItems?.Count / receiptData.Items.Count : 0f,
                    SuspiciousItems = suspiciousItems,
                    IsItemValidationFraud = invalidItemsFraud
                }
            });
        }

        [HttpPost("ai-analysis")]
        public async Task<IActionResult> GetAIAnalysis([FromForm] ImageUploadRequest request)
        {
            try
            {
                var receiptData = await _formService.ExtractDataAsync(request);
                var aiAnalysis = await _skService.AnalyzeReceiptAsync(receiptData);

                return Ok(new
                {
                    ClaimId = receiptData.ClaimId,
                    AIAnalysis = aiAnalysis,
                    ClaimData = new
                    {
                        receiptData.UserId,
                        receiptData.Amount,
                        receiptData.Merchant,
                        receiptData.Description,
                        receiptData.DateOfService
                    },
                    ItemValidation = new
                    {
                        Score = receiptData.ItemValidationScore,
                        ValidItems = receiptData.ValidItems,
                        InvalidItems = receiptData.InvalidItems,
                        Notes = receiptData.ItemValidationNotes
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to generate AI analysis", Details = ex.Message });
            }
        }

        [HttpPost("adminAnalyze")]
        public async Task<IActionResult> AnalyzeClaims([FromBody] AdminPromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
            {
                return BadRequest("Prompt is required.");
            }

            string route = await _skService.RouteAdminPromptAsync(request.Prompt);

            var allClaims = _claimDatabaseManager.GetAllClaims();

            object result;

            switch (route)
            {
                case "SharedReceiptSummary":
                    var summary = _skService.SummarizeSharedReceiptFraud(allClaims, request.Prompt);
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
                        Summary = SemanticKernelService.SummarizeByFraudTemplate(allClaims, request.Prompt)
                    };
                    break;
                case "UserAnomalySummary":
                    result = new
                    {
                        SummaryType = "UserAnomalySummary",
                        Summary = SemanticKernelService.SummarizeUserAnomalies(allClaims, request.Prompt)
                    };
                    break;
                case "HighRiskVendors":
                    result = new
                    {
                        SummaryType = "HighRiskVendors",
                        Summary = SemanticKernelService.SummarizeHighRiskVendors(allClaims, request.Prompt)
                    };
                    break;

                case "ClaimPatternClassifier":
                    var pattern = _skService.SummarizeClaimPatterns(allClaims, request.Prompt);
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
                        Summary = SemanticKernelService.SummarizeClaimTimeSpikes(allClaims, request.Prompt)
                    };
                    break;

                case "SuspiciousUserNetwork":
                    result = new
                    {
                        SummaryType = "SuspiciousUserNetwork",
                        Summary = SemanticKernelService.SummarizeSuspiciousUserLinks(allClaims, request.Prompt)
                    };
                    break;

                case "SuspiciousPatternAnalysis":
                    var suspiciousPatterns = await _skService.DetectSuspiciousPatterns(allClaims, request.Prompt);
                    result = new
                    {
                        SummaryType = "SuspiciousPatternAnalysis",
                        Summary = suspiciousPatterns
                    };
                    break;

                case "RoundAmountPattern":
                    var roundAmountAnalysis = await _skService.DetectRoundAmountPatterns(allClaims, request.Prompt);
                    result = new
                    {
                        SummaryType = "RoundAmountPattern",
                        Summary = roundAmountAnalysis
                    };
                    break;

                case "HighFrequencySubmissions":
                    var highFrequencyAnalysis = await _skService.DetectHighFrequencySubmissions(allClaims, request.Prompt);
                    result = new
                    {
                        SummaryType = "HighFrequencySubmissions",
                        Summary = highFrequencyAnalysis
                    };
                    break;

                case "UnusualTimingPatterns":
                    var timingPatterns = await _skService.DetectUnusualTimingPatterns(allClaims, request.Prompt);
                    result = new
                    {
                        SummaryType = "UnusualTimingPatterns",
                        Summary = timingPatterns
                    };
                    break;

                case "RapidSuccessionClaims":
                    var rapidSuccession = await _skService.DetectRapidSuccessionClaims(allClaims, request.Prompt);
                    result = new
                    {
                        SummaryType = "RapidSuccessionClaims",
                        Summary = rapidSuccession
                    };
                    break;

                case "IPAnomalies":
                    var ipAnomalies = await _skService.DetectIPAnomalies(allClaims, request.Prompt);
                    result = new
                    {
                        SummaryType = "IPAnomalies",
                        Summary = ipAnomalies
                    };
                    break;

                case "EscalatingAmounts":
                    var escalatingAmounts = await _skService.DetectEscalatingAmounts(allClaims, request.Prompt);
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

        [HttpGet("hsa-items")]
        public IActionResult GetHsaEligibleItems()
        {
            try
            {
                if (_formService is FormRecognizerService formService)
                {
                    var allowedItems = formService.GetAllowedHsaItems().OrderBy(x => x).ToList();
                    
                    return Ok(new
                    {
                        TotalCount = allowedItems.Count,
                        Items = allowedItems,
                        Categories = new
                        {
                            Medications = allowedItems.Where(x => x.Contains("medication", StringComparison.OrdinalIgnoreCase) || 
                                                                 x.Contains("drug", StringComparison.OrdinalIgnoreCase) ||
                                                                 x.Contains("prescription", StringComparison.OrdinalIgnoreCase)).ToList(),
                            MedicalSupplies = allowedItems.Where(x => x.Contains("bandage", StringComparison.OrdinalIgnoreCase) || 
                                                                    x.Contains("gauze", StringComparison.OrdinalIgnoreCase) ||
                                                                    x.Contains("medical", StringComparison.OrdinalIgnoreCase)).ToList(),
                            VisionCare = allowedItems.Where(x => x.Contains("eye", StringComparison.OrdinalIgnoreCase) || 
                                                               x.Contains("vision", StringComparison.OrdinalIgnoreCase) ||
                                                               x.Contains("glasses", StringComparison.OrdinalIgnoreCase)).ToList(),
                            DentalCare = allowedItems.Where(x => x.Contains("dental", StringComparison.OrdinalIgnoreCase) || 
                                                               x.Contains("tooth", StringComparison.OrdinalIgnoreCase)).ToList()
                        }
                    });
                }
                else
                {
                    return BadRequest("FormRecognizer service not available");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to retrieve HSA eligible items", Details = ex.Message });
            }
        }
    }
}

