using HSAReceiptAnalyzer.Models;

namespace HSAReceiptAnalyzer.Services.Interface
{
    public interface IFormRecognizerService
    {
       Task<Claim> ExtractDataAsync(ImageUploadRequest request);
    }
}
