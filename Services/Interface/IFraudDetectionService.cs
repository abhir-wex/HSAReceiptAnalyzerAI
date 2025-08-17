using HSAReceiptAnalyzer.Models;

namespace HSAReceiptAnalyzer.Services.Interface
{
    public interface IFraudDetectionService
    {
        string TrainModel();
        FraudResult Predict(Claim claim);
    }
}
