using HSAReceiptAnalyzer.Data;
using HSAReceiptAnalyzer.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HSAReceiptAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimDatabaseController : ControllerBase
    {
        private readonly IClaimDatabaseManager _manager;
        private readonly ClaimDatabaseOptions _options;

        public ClaimDatabaseController(
            IClaimDatabaseManager claimDatabaseManager,
            IOptions<ClaimDatabaseOptions> options)
        {
            _manager = claimDatabaseManager;
            _options = options.Value;
            // Now you can use _options.DbPath and _options.JsonPath
        }

        [HttpPost("initialize")]
        public IActionResult InitializeDatabase()
        {
            _manager.InitializeDatabase();
            return Ok("Database initialized successfully.");
        }
    }

    public class ClaimDatabaseOptions
    {
        public string DbPath { get; set; }
        public string JsonPath { get; set; }
    }
}
