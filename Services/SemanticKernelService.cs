using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.Data.Sqlite;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Sprache;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
namespace HSAReceiptAnalyzer.Services
{
    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly KernelFunction _fraudFunction;
        private readonly KernelFunction _adminPromptRouterFunction, _summaryFunction; 
        private readonly IClaimDatabaseManager _claimDatabaseManager;

        public SemanticKernelService(IConfiguration config, IClaimDatabaseManager claimDatabaseManager)
        {
            _claimDatabaseManager = claimDatabaseManager;

            // Create a KernelBuilder instance
            var builder = Kernel.CreateBuilder();

            // Use your model ID (e.g., "azure-gpt-4o" or "bedrock-titan-text-lite-v1")
            string modelId = "azure-gpt-4o";

            // Get configuration from environment variables first, then fall back to appsettings.json
            var wexConfig = config.GetSection("WEXOpenAI");
            string endpoint = Environment.GetEnvironmentVariable("WEX_OPENAI_ENDPOINT") ?? 
                wexConfig["Endpoint"] ?? 
                throw new InvalidOperationException("WEX OpenAI endpoint not configured");

            string apiKey = Environment.GetEnvironmentVariable("WEX_OPENAI_KEY") ??
                wexConfig["Key"] ?? 
                throw new InvalidOperationException("WEX OpenAI API key not configured");

            // Validate that we have actual values (not empty strings)
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("WEX OpenAI endpoint is empty. Please configure WEX_OPENAI_ENDPOINT environment variable or WEXOpenAI:Endpoint in appsettings.json");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("WEX OpenAI API key is empty. Please configure WEX_OPENAI_KEY environment variable or WEXOpenAI:Key in appsettings.json");

            if (apiKey.Contains("YOUR_WEX_API_KEY_HERE"))
                throw new InvalidOperationException("SemanticKernelService: WEX OpenAI API key contains placeholder text. Received: " + apiKey);

            // Convert endpoint string to Uri
            var endpointUri = new Uri(endpoint);

            // Set up OpenAI-compatible chat completion using the builder
            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                endpoint: endpointUri // Pass Uri instead of string
            );

            // Build the kernel
            _kernel = builder.Build();

            // Enhanced fraud analysis prompt for better user-facing responses
            string prompt = @"
You are an expert HSA (Health Savings Account) fraud detection assistant analyzing a medical claim.

**Claim Information:**
- Date of Service: {{$Date}}
- Amount: ${{$Amount}}
- Merchant: {{$Merchant}}
- Description: {{$Description}}
- Submission Date: {{$SubmissionDate}}
- Patient Name: {{$Name}}
- Items: {{$Items}}
- Flags: {{$Flags}}
- Is Fraudulent (ML): {{$IsFradulent}}

**Historical Context:**
{{$History}}

**Your Analysis Tasks:**
1. **Claim Summary**: Summarize the claim in clear, professional language
2. **Risk Assessment**: Evaluate fraud risk based on the data provided
3. **Pattern Detection**: Identify suspicious patterns such as:
   - Duplicate receipts across multiple users
   - Unusual amounts for the service type
   - High frequency of claims in short periods
   - Geographic inconsistencies
   - Suspicious vendor/merchant relationships
   - Round dollar amounts that may indicate fabrication
4. **HSA Eligibility**: Assess if items/services are HSA-eligible
5. **Recommendations**: Suggest next steps for claims reviewers

**Response Format:**
Provide your analysis in a clear, professional format that can be read by both claims reviewers and customers. Use bullet points for key findings and include a confidence level (Low/Medium/High) for your assessment.

**Example Response Structure:**
## Claim Analysis Summary
[Brief overview of the claim]

## Risk Assessment: [Low/Medium/High Risk]
[Explanation of risk level and reasoning]

## Key Findings:
• [Finding 1]
• [Finding 2]
• [Finding 3]

## HSA Eligibility Assessment:
[Assessment of whether items are HSA-eligible]

## Recommended Actions:
[Specific recommendations for claims processing]

Please provide a thorough but concise analysis.
";

            // Initialize _fraudFunction in a local variable and assign it to the readonly field
            var fraudFunction = _kernel.CreateFunctionFromPrompt(prompt);
            _fraudFunction = fraudFunction;

            var adminPrompt = @"
You're an expert in detecting fraud patterns in HSA claims. Given a prompt, decide the type of summary needed.
Return only one of the following keywords based on the prompt content:

- SharedReceiptSummary: For shared receipts, duplicate receipts across users
- TemplateSummary: For fraud template analysis, grouping by fraud types
- UserAnomalySummary: For high-frequency users (users with many claims)
- SuspiciousPatternAnalysis: For behavioral patterns, unusual spending, round amounts, timing anomalies, escalating amounts, rapid submissions, IP sharing
- ClaimTimeSpikeSummary: For time-based spikes and bulk submissions
- RoundAmountPattern: Specifically for round amount analysis only
- HighFrequencySubmissions: For same-day multiple submissions analysis
- UnusualTimingPatterns: For late night and weekend submission patterns
- RapidSuccessionClaims: For claims submitted in rapid succession
- IPAnomalies: For shared IP address analysis
- EscalatingAmounts: For escalating claim amount patterns

Guidelines:
- If prompt mentions 'round amounts' specifically → use RoundAmountPattern
- If prompt mentions 'high frequency', 'same day', 'multiple submissions' → use HighFrequencySubmissions
- If prompt mentions 'timing', 'late night', 'weekend', 'unusual hours' → use UnusualTimingPatterns
- If prompt mentions 'rapid', 'succession', 'quick submissions' → use RapidSuccessionClaims
- If prompt mentions 'IP', 'geographic', 'address sharing' → use IPAnomalies
- If prompt mentions 'escalating', 'increasing amounts', 'growing claims' → use EscalatingAmounts
- If prompt mentions 'comprehensive', 'all patterns', 'behavioral analysis' → use SuspiciousPatternAnalysis
- If prompt mentions 'frequent claimers', 'many claims' → use UserAnomalySummary
- If prompt mentions 'shared receipts', 'duplicate receipts' → use SharedReceiptSummary
- If prompt mentions 'fraud templates', 'fraud types' → use TemplateSummary
- If prompt mentions 'time spikes', 'bulk submissions', 'same day' → use ClaimTimeSpikeSummary
- If prompt specifically mentions 'round amounts' only → use RoundAmountPattern
- If prompt mentions 'Yes, please' -> generate a report of the last response

Patterns to detect:
- Repeated claim amounts or rounding (e.g., 100.00, 50.00)
- Regular time intervals between claims
- Frequent claims at same location or vendor
- Multiple claims for same service type in a short time
- Sudden spikes in claim activity by a single user or vendor
- Unusual submission times (e.g., late nights, holidays)
- Bulk claims at month-end or weekends
- Shared addresses, emails, or payment methods
- Use of the same vendor or receipt across users
- Timing overlaps in claim submissions

Prompt: {{$input}}
";

            _adminPromptRouterFunction = _kernel.CreateFunctionFromPrompt(adminPrompt); // <-- Store as field

            // AI Summary function for analyzing JSON results
            var summaryPrompt = @"You are an expert fraud analyst.

Analyze the dataset provided below and produce your findings in **Markdown** format with clear headings, bullet points, and concise explanations.

---

### Data Type
{{$DataType}}

### Original Prompt
{{$OriginalPrompt}}

Please provide the following sections in your response:

Key Insights and Patterns – Describe the most notable fraud patterns, repeated behaviors, and any clustering in the data.

Risk Assessment and Severity Levels – Categorize risks as High, Medium, or Low, with reasoning for each.

Actionable Recommendations – Practical steps for detection, prevention, and investigation.

Important Trends or Anomalies – Highlight unusual spikes, user clusters, or behavior shifts.

Clear, Executive-Level Summary – A concise overview for leadership.

Make it as a readable response.

End your response with the question:
""Do you want me to generate a comprehensive report for further distribution and analysis?""
";

            _summaryFunction = _kernel.CreateFunctionFromPrompt(summaryPrompt);
        }

        public async Task<string> AnalyzeReceiptAsync(Claim data)
        {
            // Pass the connection and UserId to GetClaims
            var historyClaims = _claimDatabaseManager.GetAllClaims();
            var historySummary = BuildHistorySummary(historyClaims);

            var arguments = new KernelArguments
            {
                ["Date"] = data.DateOfService.ToString("yyyy-MM-dd"),
                ["Amount"] = data.Amount.ToString("F2"),
                ["Merchant"] = data.Merchant ?? "Unknown",
                ["Description"] = data.Description ?? "No description provided",
                ["SubmissionDate"] = data.SubmissionDate.ToString("yyyy-MM-dd HH:mm"),
                ["Name"] = data.Name ?? "Unknown",
                ["Flags"] = data.Flags ?? "None",
                ["IsFradulent"] = data.IsFraudulent.ToString(),
                ["Items"] = string.Join(", ", data.Items ?? new List<string>()),
                ["History"] = historySummary
            };

            var result = await _fraudFunction.InvokeAsync(_kernel, arguments);
            return result.ToString();
        }

        public string BuildHistorySummary(List<Claim> claims)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Recent Claims History (Last 10 claims):");
            
            var recentClaims = claims
                .OrderByDescending(c => c.SubmissionDate)
                .Take(10)
                .ToList();
                
            if (!recentClaims.Any())
            {
                sb.AppendLine("No previous claims found.");
                return sb.ToString();
            }
            
            foreach (var claim in recentClaims)
            {
                var fraudStatus = claim.IsFraudulent == 1 ? "FRAUDULENT" : "Normal";
                sb.AppendLine($"- Date: {claim.DateOfService:yyyy-MM-dd}, Amount: ${claim.Amount:F2}, Merchant: {claim.Merchant}, Status: {fraudStatus}");
            }
            return sb.ToString();
        }

        public async Task<string> RouteAdminPromptAsync(string prompt)
        {
            var variables = new KernelArguments
            {
                ["input"] = prompt // <-- Use the correct variable name for the template
            };

            try
            {
                var result = await _adminPromptRouterFunction.InvokeAsync(_kernel, variables); // <-- Use the field
                string classification = result?.ToString()?.Trim();

                var validRoutes = new[] { 
                    "SharedReceiptSummary", 
                    "TemplateSummary", 
                    "UserAnomalySummary", 
                    "SuspiciousPatternAnalysis", 
                    "ClaimTimeSpikeSummary", 
                    "RoundAmountPattern",
                    "HighFrequencySubmissions",
                    "UnusualTimingPatterns", 
                    "RapidSuccessionClaims",
                    "IPAnomalies",
                    "EscalatingAmounts",
                    "GenerateReport"
                };
                if (validRoutes.Contains(classification))
                {
                    return classification;
                }
                return "Unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Prompt routing failed: {ex.Message}");
                return "Unknown";
            }
        }


        public object SummarizeSharedReceiptFraud(List<Claim> claims, string prompt)
        {
            var result = claims
                .GroupBy(c => c.ReceiptId)
                .Where(g => g.Select(c => c.UserId).Distinct().Count() > 1)
                .Select(g => new SharedReceiptFraudResult
                {
                    ReceiptId = g.Key,
                    Users = g.Select(c => c.UserId).Distinct().ToList(),
                    FraudTemplate = g.FirstOrDefault()?.FraudTemplate ?? string.Empty
                }).ToList();

            var jsonData = System.Text.Json.JsonSerializer.Serialize(result);
            var aiSummary = GetAISummaryAsync("SharedReceiptSummary", prompt, jsonData).Result; // Use .Result to get the string value

            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "SharedReceiptSummary",
                Results = result,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;
        }

        public static object SummarizeByFraudTemplate(List<Claim> claims, string prompt)
        {
            var result = claims
                .Where(c => c.IsFraudulent == 1)
                .GroupBy(c => c.FraudTemplate)
                .Select(g => new
                {
                    FraudTemplate = g.Key,
                    ClaimCount = g.Count(),
                    Users = g.Select(c => c.UserId).Distinct().Count()
                }).ToList();

            return new { Prompt = prompt, Type = "TemplateSummary", Results = result };
        }

        public static object SummarizeUserAnomalies(List<Claim> claims, string prompt)
        {
            var frequentClaimers = claims
                .GroupBy(c => c.UserId)
                .Where(g => g.Count() > 30)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalClaims = g.Count(),
                    AvgAmount = g.Average(c => c.Amount),
                    Flags = "HighClaimFrequency"
                }).ToList();

            return new { Prompt = prompt, Type = "UserAnomalySummary", Results = frequentClaimers };
        }

        public static object SummarizeHighRiskVendors(List<Claim> claims, string prompt)
        {
            var vendorGroups = claims
                .GroupBy(c => c.Merchant)
                .Select(g => new
                {
                    Vendor = g.Key,
                    TotalClaims = g.Count(),
                    DistinctUsers = g.Select(c => c.UserId).Distinct().Count(),
                    FraudulentClaims = g.Count(c => c.IsFraudulent == 1),
                    FraudRate = g.Any() ? (double)g.Count(c => c.IsFraudulent == 1) / g.Count() : 0
                })
                .Where(g => g.TotalClaims > 3 && (g.FraudulentClaims > 1 || g.FraudRate > 0.3))
                .OrderByDescending(g => g.FraudulentClaims)
                .ToList();

            return new { Prompt = prompt, Type = "HighRiskVendors", Results = vendorGroups };
        }

        public async Task<object> SummarizeClaimPatterns(List<Claim> claims, string prompt)
        {
            var patternGroups = claims
                .GroupBy(c => new { c.Amount, c.Merchant, c.ServiceType })
                .Where(g => g.Count() > 2)
                .Select(g => new SharedReceiptFraudResult
                {
                    ReceiptId = string.Empty, // Use empty string instead of null
                    Users = g.Select(c => c.UserId).Distinct().ToList(),
                    FraudTemplate = string.Empty // No FraudTemplate in the anonymous type, set to empty or appropriate value
                })
                .OrderByDescending(g => g.Users.Count)
                .ToList();

            var jsonData = System.Text.Json.JsonSerializer.Serialize(patternGroups);
            var aiSummary = await GetAISummaryAsync("ClaimPatternClassifier", prompt, jsonData);

            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "ClaimPatternClassifier",
                Results = patternGroups,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;
        }

        public static object SummarizeClaimTimeSpikes(List<Claim> claims, string prompt)
        {
            var spikes = claims
                .GroupBy(c => new { c.UserId, Date = c.SubmissionDate })
                .Where(g => g.Count() >= 3) // 3 or more claims on same day
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    Date = g.Key.Date,
                    ClaimsOnThatDay = g.Count(),
                    Vendors = g.Select(c => c.Merchant).Distinct().ToList()
                })
                .OrderByDescending(g => g.ClaimsOnThatDay)
                .ToList();

            return new { Prompt = prompt, Type = "ClaimTimeSpikeSummary", Results = spikes };
        }

        public static object SummarizeSuspiciousUserLinks(List<Claim> claims, string prompt)
        {
            var suspiciousLinks = claims
                .GroupBy(c => c.Address) // or .Email or .PaymentMethod
                .Where(g => g.Select(c => c.UserId).Distinct().Count() > 1)
                .Select(g => new
                {
                    SharedAddress = g.Key,
                    Users = g.Select(c => c.UserId).Distinct().ToList(),
                    ClaimCount = g.Count(),
                    Vendors = g.Select(c => c.Merchant).Distinct().ToList()
                })
                .ToList();

            return new { Prompt = prompt, Type = "SuspiciousUserNetwork", Results = suspiciousLinks };
        }

        public async Task<object> DetectSuspiciousPatterns(List<Claim> claims, string prompt)
        {
            // Detect round amount patterns (e.g., $100.00, $50.00, $250.00)
            var roundAmountPatterns = claims
                .Where(c => c.Amount % 10 == 0 || c.Amount % 25 == 0 || c.Amount % 50 == 0)
                .GroupBy(c => c.Amount)
                .Where(g => g.Count() > 5)
                .Select(g => new
                {
                    Amount = g.Key,
                    Frequency = g.Count(),
                    Users = g.Select(c => c.UserId).Distinct().ToList(),
                    PatternType = "RoundAmountPattern"
                }).ToList();

            // Detect frequent same-day submissions
            var sameDaySubmissions = claims
                .GroupBy(c => new { c.UserId, Date = c.SubmissionDate.Date })
                .Where(g => g.Count() > 3)
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    SubmissionDate = g.Key.Date,
                    ClaimCount = g.Count(),
                    TotalAmount = g.Sum(c => c.Amount),
                    Merchants = g.Select(c => c.Merchant).Distinct().ToList(),
                    PatternType = "HighFrequencySubmission"
                }).ToList();

            // Detect unusual timing patterns (late night or weekend submissions)
            var unusualTimingPatterns = claims
                .Where(c => c.SubmissionDate.Hour < 6 || c.SubmissionDate.Hour > 22 || 
                           c.SubmissionDate.DayOfWeek == DayOfWeek.Saturday || 
                           c.SubmissionDate.DayOfWeek == DayOfWeek.Sunday)
                .GroupBy(c => c.UserId)
                .Where(g => g.Count() > 10)
                .Select(g => new
                {
                    UserId = g.Key,
                    UnusualSubmissions = g.Count(),
                    LateNightSubmissions = g.Count(c => c.SubmissionDate.Hour < 6 || c.SubmissionDate.Hour > 22),
                    WeekendSubmissions = g.Count(c => c.SubmissionDate.DayOfWeek == DayOfWeek.Saturday || 
                                                      c.SubmissionDate.DayOfWeek == DayOfWeek.Sunday),
                    PatternType = "UnusualTimingPattern"
                }).ToList();

            // Detect rapid succession claims (multiple claims within short time windows)
            var rapidSuccessionClaims = claims
                .OrderBy(c => c.SubmissionDate)
                .GroupBy(c => c.UserId)
                .SelectMany(userGroup => 
                {
                    var userClaims = userGroup.OrderBy(c => c.SubmissionDate).ToList();
                    var rapidClaims = new List<object>();
                    
                    for (int i = 0; i < userClaims.Count - 2; i++)
                    {
                        var timeSpan = userClaims[i + 2].SubmissionDate - userClaims[i].SubmissionDate;
                        if (timeSpan.TotalMinutes <= 30) // 3 claims within 30 minutes
                        {
                            rapidClaims.Add(new
                            {
                                UserId = userGroup.Key,
                                ClaimCount = 3,
                                TimeWindow = timeSpan.TotalMinutes,
                                StartTime = userClaims[i].SubmissionDate,
                                EndTime = userClaims[i + 2].SubmissionDate,
                                PatternType = "RapidSuccessionPattern"
                            });
                        }
                    }
                    return rapidClaims;
                }).ToList();

            // Detect geographic anomalies (same IP address across different users)
            var ipAnomalies = claims
                .Where(c => !string.IsNullOrEmpty(c.IPAddress))
                .GroupBy(c => c.IPAddress)
                .Where(g => g.Select(c => c.UserId).Distinct().Count() > 3)
                .Select(g => new
                {
                    IPAddress = g.Key,
                    UserCount = g.Select(c => c.UserId).Distinct().Count(),
                    Users = g.Select(c => c.UserId).Distinct().ToList(),
                    ClaimCount = g.Count(),
                    PatternType = "SharedIPPattern"
                }).ToList();

            // Detect escalating amounts pattern
            var escalatingAmounts = claims
                .GroupBy(c => c.UserId)
                .Where(g => g.Count() > 5)
                .Select(g => 
                {
                    var orderedClaims = g.OrderBy(c => c.SubmissionDate).ToList();
                    var isEscalating = true;
                    var escalationCount = 0;
                    
                    for (int i = 1; i < orderedClaims.Count; i++)
                    {
                        if (orderedClaims[i].Amount > orderedClaims[i-1].Amount)
                            escalationCount++;
                    }
                    
                    var escalationRate = (double)escalationCount / (orderedClaims.Count - 1);
                    
                    return new
                    {
                        UserId = g.Key,
                        TotalClaims = orderedClaims.Count,
                        EscalationRate = escalationRate,
                        FirstAmount = orderedClaims.First().Amount,
                        LastAmount = orderedClaims.Last().Amount,
                        AmountIncrease = orderedClaims.Last().Amount - orderedClaims.First().Amount,
                        PatternType = "EscalatingAmountPattern",
                        IsSignificant = escalationRate > 0.7 // More than 70% of claims show escalation
                    };
                })
                .Where(x => x.IsSignificant)
                .ToList();

            var suspiciousPatternResults = new
            {
                RoundAmountPatterns = roundAmountPatterns,
                SameDaySubmissions = sameDaySubmissions,
                UnusualTimingPatterns = unusualTimingPatterns,
                RapidSuccessionClaims = rapidSuccessionClaims,
                IPAnomalies = ipAnomalies,
                EscalatingAmounts = escalatingAmounts,
                Summary = new
                {
                    TotalRoundAmountPatterns = roundAmountPatterns.Count,
                    TotalHighFrequencyUsers = sameDaySubmissions.Count,
                    TotalUnusualTimingUsers = unusualTimingPatterns.Count,
                    TotalRapidSuccessionIncidents = rapidSuccessionClaims.Count,
                    TotalSharedIPIncidents = ipAnomalies.Count,
                    TotalEscalatingAmountUsers = escalatingAmounts.Count
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(suspiciousPatternResults);
            var aiSummary = await GetAISummaryAsync("SuspiciousPatternAnalysis", prompt, jsonData);

            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "SuspiciousPatternAnalysis",
               // Results = suspiciousPatternResults,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;

            //return new
            //{
            //    Prompt = prompt,
            //    Type = "SuspiciousPatternAnalysis",
            //    Results = suspiciousPatternResults,
            //    AiSummary = aiSummary
            //};
        }

        public async Task<object> DetectRoundAmountPatterns(List<Claim> claims, string prompt)
        {
            var roundAmountPatterns = claims
                .Where(c => c.Amount % 10 == 0 || c.Amount % 25 == 0 || c.Amount % 50 == 0)
                .GroupBy(c => c.Amount)
                .Where(g => g.Count() > 5)
                .Select(g => new
                {
                    Amount = g.Key,
                    Frequency = g.Count(),
                    Users = g.Select(c => c.UserId).Distinct().ToList(),
                    PatternType = "RoundAmountPattern",
                    RiskLevel = g.Count() > 20 ? "High" : g.Count() > 10 ? "Medium" : "Low"
                }).ToList();

            var results = new
            {
                RoundAmountPatterns = roundAmountPatterns,
                Summary = new
                {
                    TotalRoundAmountPatterns = roundAmountPatterns.Count,
                    HighRiskPatterns = roundAmountPatterns.Count(p => p.RiskLevel == "High"),
                    MostFrequentAmount = roundAmountPatterns.OrderByDescending(p => p.Frequency).FirstOrDefault()?.Amount ?? 0
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(results);
            var aiSummary = await GetAISummaryAsync("RoundAmountPatterns", prompt, jsonData);


            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "RoundAmountPatterns",
               // Results = result,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;

            //return new
            //{
            //    Prompt = prompt,
            //    Type = "RoundAmountPatterns",
            //    Results = results,
            //    AiSummary = aiSummary
            //};
        }

        public async Task<object> DetectHighFrequencySubmissions(List<Claim> claims, string prompt)
        {
            var sameDaySubmissions = claims
                .GroupBy(c => new { c.UserId, Date = c.SubmissionDate.Date })
                .Where(g => g.Count() > 3)
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    SubmissionDate = g.Key.Date,
                    ClaimCount = g.Count(),
                    TotalAmount = g.Sum(c => c.Amount),
                    Merchants = g.Select(c => c.Merchant).Distinct().ToList(),
                    PatternType = "HighFrequencySubmission",
                    RiskLevel = g.Count() > 10 ? "High" : g.Count() > 6 ? "Medium" : "Low"
                }).ToList();

            var results = new
            {
                HighFrequencySubmissions = sameDaySubmissions,
                Summary = new
                {
                    TotalHighFrequencyUsers = sameDaySubmissions.Count,
                    HighRiskUsers = sameDaySubmissions.Count(s => s.RiskLevel == "High"),
                    TotalSuspiciousClaims = sameDaySubmissions.Sum(s => s.ClaimCount),
                    TotalSuspiciousAmount = sameDaySubmissions.Sum(s => s.TotalAmount)
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(results);
            var aiSummary = await GetAISummaryAsync("HighFrequencySubmissions", prompt, jsonData);

            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "HighFrequencySubmissions",
                // Results = result,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;

            //return new
            //{
            //    Prompt = prompt,
            //    Type = "HighFrequencySubmissions",
            //    Results = results,
            //    AiSummary = aiSummary
            //};
        }

        public async Task<object> DetectUnusualTimingPatterns(List<Claim> claims, string prompt)
        {
            var unusualTimingPatterns = claims
                .Where(c => c.SubmissionDate.Hour < 6 || c.SubmissionDate.Hour > 22 || 
                           c.SubmissionDate.DayOfWeek == DayOfWeek.Saturday || 
                           c.SubmissionDate.DayOfWeek == DayOfWeek.Sunday)
                .GroupBy(c => c.UserId)
                .Where(g => g.Count() > 10)
                .Select(g => new
                {
                    UserId = g.Key,
                    UnusualSubmissions = g.Count(),
                    LateNightSubmissions = g.Count(c => c.SubmissionDate.Hour < 6 || c.SubmissionDate.Hour > 22),
                    WeekendSubmissions = g.Count(c => c.SubmissionDate.DayOfWeek == DayOfWeek.Saturday || 
                                                      c.SubmissionDate.DayOfWeek == DayOfWeek.Sunday),
                    PatternType = "UnusualTimingPattern",
                    RiskLevel = g.Count() > 50 ? "High" : g.Count() > 25 ? "Medium" : "Low"
                }).ToList();

            var results = new
            {
                UnusualTimingPatterns = unusualTimingPatterns,
                Summary = new
                {
                    TotalUnusualTimingUsers = unusualTimingPatterns.Count,
                    TotalLateNightSubmissions = unusualTimingPatterns.Sum(u => u.LateNightSubmissions),
                    TotalWeekendSubmissions = unusualTimingPatterns.Sum(u => u.WeekendSubmissions),
                    HighRiskUsers = unusualTimingPatterns.Count(u => u.RiskLevel == "High")
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(results);
            var aiSummary = await GetAISummaryAsync("UnusualTimingPatterns", prompt, jsonData);

            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "UnusualTimingPatterns",
                // Results = result,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;

            //return new
            //{
            //    Prompt = prompt,
            //    Type = "UnusualTimingPatterns",
            //    Results = results,
            //    AiSummary = aiSummary
            //};
        }

        public async Task<object> DetectRapidSuccessionClaims(List<Claim> claims, string prompt)
        {
            var rapidSuccessionClaims = claims
                .OrderBy(c => c.SubmissionDate)
                .GroupBy(c => c.UserId)
                .SelectMany(userGroup => 
                {
                    var userClaims = userGroup.OrderBy(c => c.SubmissionDate).ToList();
                    var rapidClaims = new List<object>();
                    
                    for (int i = 0; i < userClaims.Count - 2; i++)
                    {
                        var timeSpan = userClaims[i + 2].SubmissionDate - userClaims[i].SubmissionDate;
                        if (timeSpan.TotalMinutes <= 30) // 3 claims within 30 minutes
                        {
                            rapidClaims.Add(new
                            {
                                UserId = userGroup.Key,
                                ClaimCount = 3,
                                TimeWindow = timeSpan.TotalMinutes,
                                StartTime = userClaims[i].SubmissionDate,
                                EndTime = userClaims[i + 2].SubmissionDate,
                                PatternType = "RapidSuccessionPattern",
                                RiskLevel = timeSpan.TotalMinutes <= 5 ? "High" : timeSpan.TotalMinutes <= 15 ? "Medium" : "Low"
                            });
                        }
                    }
                    return rapidClaims;
                }).ToList();

            var results = new
            {
                RapidSuccessionClaims = rapidSuccessionClaims,
                Summary = new
                {
                    TotalRapidSuccessionIncidents = rapidSuccessionClaims.Count,
                    HighRiskIncidents = rapidSuccessionClaims.Count(r => ((dynamic)r).RiskLevel == "High"),
                    AverageTimeWindow = rapidSuccessionClaims.Any() ? rapidSuccessionClaims.Average(r => ((dynamic)r).TimeWindow) : 0
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(results);
            var aiSummary = await GetAISummaryAsync("RapidSuccessionClaims", prompt, jsonData);

            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "RapidSuccessionClaims",
                // Results = result,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;

            //return new
            //{
            //    Prompt = prompt,
            //    Type = "RapidSuccessionClaims",
            //    Results = results,
            //    AiSummary = aiSummary
            //};
        }

        public async Task<object> DetectIPAnomalies(List<Claim> claims, string prompt)
        {
            var ipAnomalies = claims
                .Where(c => !string.IsNullOrEmpty(c.IPAddress))
                .GroupBy(c => c.IPAddress)
                .Where(g => g.Select(c => c.UserId).Distinct().Count() > 3)
                .Select(g => new
                {
                    IPAddress = g.Key,
                    UserCount = g.Select(c => c.UserId).Distinct().Count(),
                    Users = g.Select(c => c.UserId).Distinct().ToList(),
                    ClaimCount = g.Count(),
                    PatternType = "SharedIPPattern",
                    RiskLevel = g.Select(c => c.UserId).Distinct().Count() > 10 ? "High" : 
                               g.Select(c => c.UserId).Distinct().Count() > 6 ? "Medium" : "Low"
                }).ToList();

            var results = new
            {
                IPAnomalies = ipAnomalies,
                Summary = new
                {
                    TotalSharedIPIncidents = ipAnomalies.Count,
                    TotalSuspiciousIPs = ipAnomalies.Count,
                    HighRiskIPs = ipAnomalies.Count(ip => ip.RiskLevel == "High"),
                    TotalUsersAffected = ipAnomalies.SelectMany(ip => ip.Users).Distinct().Count()
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(results);
            var aiSummary = await GetAISummaryAsync("IPAnomalies", prompt, jsonData);

            var fraudResult = new SharedFraudReceiptSummary
            {
                Prompt = prompt,
                Type = "IPAnomalies",
                // Results = result,
                AiSummary = aiSummary
            };

            return fraudResult.AiSummary;

            //return new
            //{
            //    Prompt = prompt,
            //    Type = "IPAnomalies",
            //    Results = results,
            //    AiSummary = aiSummary
            //};
        }

        public async Task<object> DetectEscalatingAmounts(List<Claim> claims, string prompt)
        {
            var escalatingAmounts = claims
                .GroupBy(c => c.UserId)
                .Where(g => g.Count() > 5)
                .Select(g => 
                {
                    var orderedClaims = g.OrderBy(c => c.SubmissionDate).ToList();
                    var escalationCount = 0;
                    
                    for (int i = 1; i < orderedClaims.Count; i++)
                    {
                        if (orderedClaims[i].Amount > orderedClaims[i-1].Amount)
                            escalationCount++;
                    }
                    
                    var escalationRate = (double)escalationCount / (orderedClaims.Count - 1);
                    
                    return new
                    {
                        UserId = g.Key,
                        TotalClaims = orderedClaims.Count,
                        EscalationRate = escalationRate,
                        FirstAmount = orderedClaims.First().Amount,
                        LastAmount = orderedClaims.Last().Amount,
                        AmountIncrease = orderedClaims.Last().Amount - orderedClaims.First().Amount,
                        PatternType = "EscalatingAmountPattern",
                        RiskLevel = escalationRate > 0.8 ? "High" : escalationRate > 0.6 ? "Medium" : "Low",
                        IsSignificant = escalationRate > 0.7
                    };
                })
                .Where(x => x.IsSignificant)
                .ToList();

            var results = new
            {
                EscalatingAmounts = escalatingAmounts,
                Summary = new
                {
                    TotalEscalatingAmountUsers = escalatingAmounts.Count,
                    HighRiskUsers = escalatingAmounts.Count(e => e.RiskLevel == "High"),
                    AverageEscalationRate = escalatingAmounts.Any() ? escalatingAmounts.Average(e => e.EscalationRate) : 0,
                    TotalAmountIncrease = escalatingAmounts.Sum(e => e.AmountIncrease)
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(results);
            var aiSummary = await GetAISummaryAsync("EscalatingAmounts", prompt, jsonData);

            return new
            {
                Prompt = prompt,
                Type = "EscalatingAmounts",
                Results = results,
                AiSummary = aiSummary
            };
        }

        public async Task<object> DetectSuspiciousPatternsOrchestrator(List<Claim> claims, string prompt)
        {
            // Now this method orchestrates all the individual pattern detection methods
            var roundAmountTask = DetectRoundAmountPatterns(claims, prompt);
            var highFrequencyTask = DetectHighFrequencySubmissions(claims, prompt);
            var timingTask = DetectUnusualTimingPatterns(claims, prompt);
            var rapidTask = DetectRapidSuccessionClaims(claims, prompt);
            var ipTask = DetectIPAnomalies(claims, prompt);
            var escalatingTask = DetectEscalatingAmounts(claims, prompt);

            // Wait for all tasks to complete
            await Task.WhenAll(roundAmountTask, highFrequencyTask, timingTask, rapidTask, ipTask, escalatingTask);

            var combinedResults = new
            {
                RoundAmountPatterns = ((dynamic)roundAmountTask.Result).Results,
                HighFrequencySubmissions = ((dynamic)highFrequencyTask.Result).Results,
                UnusualTimingPatterns = ((dynamic)timingTask.Result).Results,
                RapidSuccessionClaims = ((dynamic)rapidTask.Result).Results,
                IPAnomalies = ((dynamic)ipTask.Result).Results,
                EscalatingAmounts = ((dynamic)escalatingTask.Result).Results,
                OverallSummary = new
                {
                    TotalPatternTypes = 6,
                    AnalyzedClaims = claims.Count,
                    AnalysisTimestamp = DateTime.UtcNow
                }
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(combinedResults);
            var aiSummary = await GetAISummaryAsync("ComprehensiveSuspiciousPatternAnalysis", prompt, jsonData);

            return new
            {
                Prompt = prompt,
                Type = "ComprehensiveSuspiciousPatternAnalysis",
                Results = combinedResults,
                AiSummary = aiSummary
            };
        }

        private async Task<string> GetAISummaryAsync(string dataType, string originalPrompt, string jsonData)
        {
            var variables = new KernelArguments
            {
                ["DataType"] = dataType,
                ["OriginalPrompt"] = originalPrompt,
                ["JsonData"] = jsonData
            };

            try
            {
                var result = await _summaryFunction.InvokeAsync(_kernel, variables);
                return result?.ToString() ?? "Unable to generate AI summary.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Summary generation failed: {ex.Message}");
                return $"AI Summary generation failed: {ex.Message}";
            }
        }
    }

}









