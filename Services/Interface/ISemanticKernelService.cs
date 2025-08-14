using HSAReceiptAnalyzer.Models;

namespace HSAReceiptAnalyzer.Services.Interface
{
    public interface ISemanticKernelService
    {
        Task<string> AnalyzeReceiptAsync(Claim data);
        Task<string> RouteAdminPromptAsync(string prompt);
        object SummarizeSharedReceiptFraud(List<Claim> claims, string prompt);
        Task<object> SummarizeClaimPatterns(List<Claim> claims, string prompt);
        Task<object> DetectSuspiciousPatterns(List<Claim> claims, string prompt);
        
        // New specific pattern detection methods
        Task<object> DetectRoundAmountPatterns(List<Claim> claims, string prompt);
        Task<object> DetectHighFrequencySubmissions(List<Claim> claims, string prompt);
        Task<object> DetectUnusualTimingPatterns(List<Claim> claims, string prompt);
        Task<object> DetectRapidSuccessionClaims(List<Claim> claims, string prompt);
        Task<object> DetectIPAnomalies(List<Claim> claims, string prompt);
        Task<object> DetectEscalatingAmounts(List<Claim> claims, string prompt);
    }
}
