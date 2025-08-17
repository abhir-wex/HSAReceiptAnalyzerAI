using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using System.Data.SqlTypes;

namespace HSAReceiptAnalyzer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FraudController : ControllerBase
    {
        //private readonly AnomalyScorer _scorer;
        private readonly IClaimDatabaseManager _claimDatabaseManager;
        private readonly IFraudDetectionService _fraudDetectionService;

        public FraudController(IClaimDatabaseManager claimDatabaseManager, IFraudDetectionService fraudDetectionService)
        {
            _claimDatabaseManager = claimDatabaseManager;
            _fraudDetectionService = fraudDetectionService;
        }

        //[HttpPost("score")]
        //public IActionResult Score([FromBody] ClaimFeatures features)
        //{
        //    if (features == null) return BadRequest("Missing features");
        //    var result = _scorer.Score(features);

        //    // pick a threshold using validation data; e.g., top 5% scores -> anomaly
        //    bool suspect = result.IsAnomaly || result.Score > 3.0f;

        //    return Ok(new
        //    {
        //        result.IsAnomaly,
        //        result.Score,
        //        Suspect = suspect,
        //        Message = suspect ? "Potential fraud – route to manual review" : "Looks normal"
        //    });
        //}

        [HttpPost("train")]
        public async Task<IActionResult> TrainFraudModel()
        {
            try
            {
                var modelPath = await _fraudDetectionService.TrainModelAsync();
                return Ok(new
                {
                    Message = "Fraud detection model trained successfully.",
                    ModelPath = modelPath
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("train-sync")]
        public IActionResult TrainFraudModelSync()
        {
            try
            {
                var modelPath = _fraudDetectionService.TrainModel();
                return Ok(new
                {
                    Message = "Fraud detection model trained successfully.",
                    ModelPath = modelPath
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
