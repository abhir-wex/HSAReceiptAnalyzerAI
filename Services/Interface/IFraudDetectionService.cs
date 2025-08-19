using HSAReceiptAnalyzer.Models;

namespace HSAReceiptAnalyzer.Services.Interface
{
    public interface IFraudDetectionService
    {
        string TrainModel();
        Task<string> TrainModelAsync();
        FraudResult Predict(Claim claim);
        Task<FraudResult> PredictWithAIAnalysisAsync(Claim claim);
    }
}
