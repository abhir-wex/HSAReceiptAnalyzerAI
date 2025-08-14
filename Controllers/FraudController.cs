using HSAReceiptAnalyzer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HSAReceiptAnalyzer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FraudController : ControllerBase
    {
        private readonly AnomalyScorer _scorer;


        public FraudController(AnomalyScorer scorer)
        {
            _scorer = scorer;
        }

        [HttpPost("score")]
        public IActionResult Score([FromBody] ClaimFeatures features)
        {
            if (features == null) return BadRequest("Missing features");
            var result = _scorer.Score(features);

            // pick a threshold using validation data; e.g., top 5% scores -> anomaly
            bool suspect = result.IsAnomaly || result.Score > 3.0f;

            return Ok(new
            {
                result.IsAnomaly,
                result.Score,
                Suspect = suspect,
                Message = suspect ? "Potential fraud – route to manual review" : "Looks normal"
            });
        }

        [HttpPost("train")]
        public async Task<IActionResult> TrainModel([FromBody] List<ClaimFeatures>? trainingData = null)
        {
            try
            {
                if (trainingData == null || !trainingData.Any())
                {
                    return BadRequest("Training data is required and cannot be empty");
                }

                // Validate training data has labeled examples
                if (!trainingData.Any(x => x.IsFraudulent))
                {
                    return BadRequest("Training data must contain both fraudulent and non-fraudulent examples");
                }

                var sqlitePath = "ClaimsDB1.sqlite";
                var modelPath = "Data/anomaly_model.zip";

                await Task.Run(() => TrainAnomalyModel.Run(sqlitePath, modelPath));

                return Ok(new
                {
                    Message = "Model training completed successfully",
                    TrainingDataCount = trainingData.Count,
                    FraudulentExamples = trainingData.Count(x => x.IsFraudulent),
                    NormalExamples = trainingData.Count(x => !x.IsFraudulent)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Model training failed",
                    Error = ex.Message
                });
            }
        }
    }
}
