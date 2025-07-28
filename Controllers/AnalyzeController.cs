using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace HSAReceiptAnalyzer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalyzeController : ControllerBase
    {
        private readonly SemanticKernelService _skService;
        private readonly FormRecognizerService _formService;

        public AnalyzeController(SemanticKernelService skService, FormRecognizerService formService)
        {
            _skService = skService;
            _formService = formService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> AnalyzeImage([FromForm] ImageUploadRequest request)
        {
            var receiptData = await _formService.ExtractDataAsync(request.Image);
            var result = await _skService.AnalyzeReceiptAsync(receiptData);
            return Ok(result);
        }
    }
}
