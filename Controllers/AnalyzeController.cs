using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace HSAReceiptAnalyzer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalyzeController : ControllerBase
    {
        private readonly ISemanticKernelService _skService;
        private readonly IFormRecognizerService _formService;
        private readonly IClaimDatabaseManager _claimDatabaseManager;

        public AnalyzeController(ISemanticKernelService skService, IFormRecognizerService formService, IClaimDatabaseManager claimDatabaseManager
            )
        {
            _skService = skService;
            _formService = formService;
            _claimDatabaseManager = claimDatabaseManager;

        }

        [HttpPost("upload")]
        public async Task<IActionResult> AnalyzeImage([FromForm] ImageUploadRequest request)
        {
            var receiptData = await _formService.ExtractDataAsync(request.Image);
            // 2. Save claim in DB
            _claimDatabaseManager.InsertClaim(receiptData);

            // 3. ML.NET Fraud detection
           // var isFraud = _faudDetectionService.CheckFraud(receiptData);

            // 4. AI Insights
            var aiAnalysis = await _skService.AnalyzeReceiptAsync(receiptData);

            // 5. Combine response
            return Ok(new
            {
                ClaimId = receiptData.ClaimId,
               // IsFraudulent = isFraud,
                AIInsights = aiAnalysis
            });
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
    }
}

