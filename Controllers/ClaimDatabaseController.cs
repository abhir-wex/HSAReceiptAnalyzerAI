using HSAReceiptAnalyzer.Data;
using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
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

        [HttpGet("claims")]
        public IActionResult GetAllClaims([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string search = "", [FromQuery] string filter = "all", [FromQuery] string sortBy = "date")
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 5; // Limit max page size to 50

                var allClaims = _manager.GetAllClaims();
                
                // Transform claims for frontend compatibility first
                var formattedClaims = allClaims.Select(claim => new
                {
                    id = claim.ClaimId,
                    userId = claim.UserId,
                    date = claim.DateOfService.ToString("yyyy-MM-dd"),
                    amount = $"${claim.Amount:F2}",
                    merchant = claim.Merchant ?? "Unknown Merchant",
                    description = claim.Description ?? "No description",
                    fraudScore = CalculateFraudScore(claim),
                    status = GetStatusFromFraudScore(CalculateFraudScore(claim)),
                    submissionDate = claim.SubmissionDate,
                    isFraudulent = claim.IsFraudulent == 1,
                    fraudTemplate = claim.FraudTemplate,
                    category = claim.Category,
                    location = claim.Location
                }).ToList();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    formattedClaims = formattedClaims.Where(claim =>
                        claim.userId.ToLower().Contains(searchLower) ||
                        claim.merchant.ToLower().Contains(searchLower) ||
                        claim.description.ToLower().Contains(searchLower)
                    ).ToList();
                }

                // Apply risk level filter
                if (filter != "all")
                {
                    formattedClaims = formattedClaims.Where(claim =>
                    {
                        return filter switch
                        {
                            "fraud" => claim.fraudScore > 70,
                            "suspicious" => claim.fraudScore > 30 && claim.fraudScore <= 70,
                            "legit" => claim.fraudScore <= 30,
                            _ => true
                        };
                    }).ToList();
                }

                // Apply sorting
                formattedClaims = sortBy switch
                {
                    "amount" => formattedClaims.OrderByDescending(c => decimal.Parse(c.amount.Replace("$", ""))).ToList(),
                    "fraudScore" => formattedClaims.OrderByDescending(c => c.fraudScore).ToList(),
                    "date" or _ => formattedClaims.OrderByDescending(c => c.submissionDate).ToList()
                };

                // Calculate pagination
                var totalClaims = formattedClaims.Count;
                var totalPages = (int)Math.Ceiling((double)totalClaims / pageSize);
                var skip = (page - 1) * pageSize;
                var pagedClaims = formattedClaims.Skip(skip).Take(pageSize).ToList();

                // Return paginated response
                var response = new
                {
                    claims = pagedClaims,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalClaims = totalClaims,
                        totalPages = totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1,
                        nextPage = page < totalPages ? page + 1 : (int?)null,
                        previousPage = page > 1 ? page - 1 : (int?)null
                    },
                    filters = new
                    {
                        search = search,
                        filter = filter,
                        sortBy = sortBy
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to retrieve claims", 
                    message = ex.Message 
                });
            }
        }

        [HttpGet("claims/all")]
        public IActionResult GetAllClaimsNoPagination()
        {
            try
            {
                var claims = _manager.GetAllClaims();
                
                // Transform claims for frontend compatibility
                var frontendClaims = claims.Select(claim => new
                {
                    id = claim.ClaimId,
                    userId = claim.UserId,
                    date = claim.DateOfService.ToString("yyyy-MM-dd"),
                    amount = $"${claim.Amount:F2}",
                    merchant = claim.Merchant ?? "Unknown Merchant",
                    description = claim.Description ?? "No description",
                    fraudScore = CalculateFraudScore(claim),
                    status = GetStatusFromFraudScore(CalculateFraudScore(claim)),
                    submissionDate = claim.SubmissionDate,
                    isFraudulent = claim.IsFraudulent == 1,
                    fraudTemplate = claim.FraudTemplate,
                    category = claim.Category,
                    location = claim.Location
                }).OrderByDescending(c => c.submissionDate).ToList();

                return Ok(frontendClaims);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to retrieve claims", 
                    message = ex.Message 
                });
            }
        }

        [HttpPost("claims")]
        public IActionResult AddClaim([FromBody] AddClaimRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Invalid claim data" });
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.UserId) || 
                    string.IsNullOrEmpty(request.Merchant) || 
                    request.Amount <= 0)
                {
                    return BadRequest(new { error = "Missing required fields: UserId, Merchant, Amount" });
                }

                // Create new claim
                var newClaim = new Claim
                {
                    ClaimId = Guid.NewGuid().ToString(),
                    ReceiptId = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Name = request.Name ?? "Unknown",
                    Address = request.Address ?? "Unknown",
                    Merchant = request.Merchant,
                    ServiceType = request.ServiceType ?? "Healthcare",
                    Amount = request.Amount,
                    DateOfService = request.DateOfService,
                    SubmissionDate = DateTime.UtcNow,
                    Category = request.Category ?? "Medical",
                    Location = request.Location ?? "Unknown",
                    UserAge = request.UserAge > 0 ? request.UserAge : 30,
                    Items = request.Items ?? new List<string> { request.Description ?? "Service" },
                    UserGender = request.UserGender ?? "Unknown",
                    Description = request.Description ?? "HSA Expense",
                    IsFraudulent = request.FraudScore > 70 ? 1 : 0,
                    FraudTemplate = request.FraudScore > 70 ? "HighRisk" : "Normal",
                    Flags = request.FraudScore > 70 ? "HighRisk" : "Normal",
                    VendorId = request.VendorId ?? Guid.NewGuid().ToString(),
                    IPAddress = request.IPAddress ?? "127.0.0.1",
                    ReceiptHash = request.ReceiptHash ?? Guid.NewGuid().ToString()
                };

                // Insert claim into database
                _manager.InsertClaim(newClaim);

                // Return success response with the created claim
                var responseData = new
                {
                    id = newClaim.ClaimId,
                    userId = newClaim.UserId,
                    date = newClaim.DateOfService.ToString("yyyy-MM-dd"),
                    amount = $"${newClaim.Amount:F2}",
                    merchant = newClaim.Merchant,
                    description = newClaim.Description,
                    fraudScore = CalculateFraudScore(newClaim),
                    status = GetStatusFromFraudScore(CalculateFraudScore(newClaim)),
                    submissionDate = newClaim.SubmissionDate,
                    isFraudulent = newClaim.IsFraudulent == 1
                };

                return Ok(new { 
                    message = "Claim added successfully", 
                    claim = responseData 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to add claim", 
                    message = ex.Message 
                });
            }
        }

        [HttpGet("claims/stats")]
        public IActionResult GetClaimsStatistics()
        {
            try
            {
                var claims = _manager.GetAllClaims();
                
                var stats = new
                {
                    totalClaims = claims.Count,
                    fraudulentClaims = claims.Count(c => c.IsFraudulent == 1),
                    legitimateClaims = claims.Count(c => c.IsFraudulent == 0),
                    totalAmount = claims.Sum(c => c.Amount),
                    averageAmount = claims.Any() ? claims.Average(c => c.Amount) : 0,
                    recentClaims = claims.Count(c => c.SubmissionDate >= DateTime.Now.AddDays(-30)),
                    topMerchants = claims
                        .GroupBy(c => c.Merchant)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => new { merchant = g.Key, count = g.Count() })
                        .ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to retrieve statistics", 
                    message = ex.Message 
                });
            }
        }

        [HttpDelete("claims/{claimId}")]
        public IActionResult DeleteClaim(string claimId)
        {
            try
            {
                // Implement delete logic in ClaimDatabaseManager if needed
                return Ok(new { message = "Claim deletion feature not implemented yet" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to delete claim", 
                    message = ex.Message 
                });
            }
        }

        private int CalculateFraudScore(Claim claim)
        {
            // Calculate fraud score based on various factors
            int score = 0;

            // If already marked as fraudulent, return high score
            if (claim.IsFraudulent == 1)
            {
                return 85 + new Random().Next(0, 15); // 85-100 for confirmed fraud
            }

            // Calculate score based on various risk factors
            
            // Amount-based risk
            if (claim.Amount > 500) score += 15;
            else if (claim.Amount > 200) score += 8;
            else if (claim.Amount > 100) score += 3;

            // Round amount patterns (fraud indicator)
            if (claim.Amount % 50 == 0 || claim.Amount % 25 == 0) score += 10;

            // Merchant-based risk (simplified)
            if (string.IsNullOrEmpty(claim.Merchant) || claim.Merchant.ToLower().Contains("unknown"))
                score += 20;

            // Time-based risk
            var timeDiff = claim.SubmissionDate - claim.DateOfService;
            if (timeDiff.TotalDays > 30) score += 15; // Very late submission
            else if (timeDiff.TotalDays > 14) score += 8; // Late submission

            // Weekend/night submission patterns
            if (claim.SubmissionDate.DayOfWeek == DayOfWeek.Saturday || 
                claim.SubmissionDate.DayOfWeek == DayOfWeek.Sunday)
                score += 5;

            if (claim.SubmissionDate.Hour < 6 || claim.SubmissionDate.Hour > 22)
                score += 8;

            // Random variation for demonstration
            score += new Random(claim.ClaimId?.GetHashCode() ?? 0).Next(-5, 5);

            return Math.Max(0, Math.Min(100, score)); // Ensure 0-100 range
        }

        private string GetStatusFromFraudScore(int fraudScore)
        {
            if (fraudScore <= 30) return "Legit";
            if (fraudScore <= 70) return "Suspicious";
            return "Fraud";
        }
    }

    public class ClaimDatabaseOptions
    {
        public string DbPath { get; set; }
        public string JsonPath { get; set; }
    }

    public class AddClaimRequest
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Merchant { get; set; }
        public string ServiceType { get; set; }
        public double Amount { get; set; }
        public DateTime DateOfService { get; set; }
        public string Category { get; set; }
        public string Location { get; set; }
        public int UserAge { get; set; }
        public List<string> Items { get; set; }
        public string UserGender { get; set; }
        public string Description { get; set; }
        public int FraudScore { get; set; }
        public string VendorId { get; set; }
        public string IPAddress { get; set; }
        public string ReceiptHash { get; set; }
    }
}
