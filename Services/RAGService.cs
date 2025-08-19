using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using HSAReceiptAnalyzer.Services.Interface;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace HSAReceiptAnalyzer.Services
{
    public class RAGService : IRAGService
    {
        private readonly IKernelMemory _memory;
        private readonly Kernel _kernel;
        private readonly IClaimDatabaseManager _claimManager;
        private readonly ILogger<RAGService> _logger;
        private readonly string _indexName = "hsa-fraud-knowledge";
        private readonly Dictionary<string, FraudKnowledgeEntry> _localKnowledgeBase;

        public RAGService(
            Kernel kernel, 
            IClaimDatabaseManager claimManager, 
            ILogger<RAGService> logger)
        {
            _kernel = kernel;
            _claimManager = claimManager;
            _logger = logger;
            _localKnowledgeBase = new Dictionary<string, FraudKnowledgeEntry>();
            
            // Initialize KernelMemory with simple configuration
            var memoryBuilder = new KernelMemoryBuilder();
            
            try
            {
                // Use simple vector database for now
                memoryBuilder.WithSimpleVectorDb();
                
                // Build the memory instance
                _memory = memoryBuilder.Build<MemoryServerless>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize KernelMemory, using fallback local storage");
                _memory = null; // Will use local storage fallback
            }
        }

        public async Task InitializeKnowledgeBaseAsync()
        {
            try
            {
                _logger.LogInformation("Initializing RAG Knowledge Base...");

                var allClaims = _claimManager.GetAllClaims();
                var fraudClaims = allClaims.Where(c => c.IsFraudulent == 1).ToList();

                _logger.LogInformation($"Found {fraudClaims.Count} fraud cases to index");

                foreach (var claim in fraudClaims)
                {
                    await IndexFraudCaseAsync(claim);
                }

                _logger.LogInformation("RAG Knowledge Base initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RAG Knowledge Base");
                throw;
            }
        }

        public async Task IndexFraudCaseAsync(Claim claim)
        {
            try
            {
                var fraudEntry = CreateFraudKnowledgeEntry(claim);
                var documentContent = BuildDocumentContent(fraudEntry);

                // Store in local knowledge base as fallback
                _localKnowledgeBase[fraudEntry.Id] = fraudEntry;

                if (_memory != null)
                {
                    try
                    {
                        // Create tags for better search using TagCollection
                        var tags = new TagCollection();
                        tags.Add("claimId", claim.ClaimId ?? "");
                        tags.Add("fraudTemplate", claim.FraudTemplate ?? "");
                        tags.Add("merchant", claim.Merchant ?? "");
                        tags.Add("serviceType", claim.ServiceType ?? "");
                        tags.Add("amount", claim.Amount.ToString());
                        tags.Add("userId", claim.UserId ?? "");
                        tags.Add("location", claim.Location ?? "");
                        tags.Add("ipAddress", claim.IPAddress ?? "");
                        tags.Add("receiptHash", claim.ReceiptHash ?? "");

                        await _memory.ImportTextAsync(
                            text: documentContent,
                            documentId: fraudEntry.Id,
                            index: _indexName,
                            tags: tags
                        );
                    }
                    catch (Exception memEx)
                    {
                        _logger.LogWarning(memEx, "Failed to index in KernelMemory, using local storage");
                    }
                }

                _logger.LogDebug($"Indexed fraud case: {claim.ClaimId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error indexing fraud case {claim.ClaimId}");
                throw;
            }
        }

        public async Task<List<RAGSearchResult>> SearchSimilarCasesAsync(string query, int maxResults = 5)
        {
            try
            {
                if (_memory != null)
                {
                    try
                    {
                        var searchResult = await _memory.SearchAsync(
                            query: query,
                            index: _indexName,
                            limit: maxResults,
                            minRelevance: 0.6
                        );

                        return searchResult.Results.Select(result => new RAGSearchResult
                        {
                            Id = result.SourceName,
                            Content = result.Partitions.FirstOrDefault()?.Text ?? "",
                            Relevance = CalculateRelevance(result),
                            Source = "FraudKnowledgeBase",
                            Metadata = ExtractMetadata(result)
                        }).ToList();
                    }
                    catch (Exception memEx)
                    {
                        _logger.LogWarning(memEx, "KernelMemory search failed, using local search");
                    }
                }

                // Fallback to local search
                return SearchLocalKnowledgeBase(query, maxResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching similar cases for query: {query}");
                return new List<RAGSearchResult>();
            }
        }

        private List<RAGSearchResult> SearchLocalKnowledgeBase(string query, int maxResults)
        {
            var queryTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var results = new List<RAGSearchResult>();

            foreach (var entry in _localKnowledgeBase.Values)
            {
                var content = BuildDocumentContent(entry).ToLowerInvariant();
                var relevance = CalculateLocalRelevance(content, queryTerms);

                if (relevance > 0.3) // Minimum relevance threshold
                {
                    results.Add(new RAGSearchResult
                    {
                        Id = entry.Id,
                        Content = BuildDocumentContent(entry),
                        Relevance = relevance,
                        Source = "LocalKnowledgeBase",
                        Metadata = new Dictionary<string, object>
                        {
                            ["claimId"] = entry.ClaimId,
                            ["fraudTemplate"] = entry.FraudTemplate,
                            ["merchant"] = entry.Merchant,
                            ["amount"] = entry.Amount
                        }
                    });
                }
            }

            return results.OrderByDescending(r => r.Relevance).Take(maxResults).ToList();
        }

        private double CalculateLocalRelevance(string content, string[] queryTerms)
        {
            var matches = queryTerms.Count(term => content.Contains(term));
            return (double)matches / queryTerms.Length;
        }

        public async Task<RAGAnalysisResult> AnalyzeClaimWithRAGAsync(Claim claim, string? customPrompt = null)
        {
            try
            {
                // Create search query based on claim characteristics
                var searchQuery = BuildSearchQuery(claim);
                
                // Search for similar fraud cases
                var similarCases = await SearchSimilarCasesAsync(searchQuery);

                // Build context from similar cases
                var contextualInfo = BuildContextualInformation(similarCases);

                // Create analysis prompt
                var analysisPrompt = customPrompt ?? BuildAnalysisPrompt(claim, contextualInfo);

                // Get AI analysis
                var analysisResult = await _kernel.InvokePromptAsync(analysisPrompt);

                // Identify risk factors
                var riskFactors = IdentifyRiskFactors(claim, similarCases);

                // Generate recommended actions
                var recommendedAction = GenerateRecommendedActions(claim, similarCases, riskFactors);

                return new RAGAnalysisResult
                {
                    Query = searchQuery,
                    Analysis = analysisResult.ToString(),
                    SimilarCases = similarCases,
                    RiskFactors = riskFactors,
                    RecommendedAction = recommendedAction,
                    ConfidenceScore = CalculateConfidenceScore(similarCases),
                    AnalysisTimestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing claim with RAG: {claim.ClaimId}");
                throw;
            }
        }

        public async Task<string> GetContextualAnalysisAsync(string adminQuery, List<Claim> claims)
        {
            try
            {
                // Search knowledge base for relevant historical cases
                var relevantCases = await SearchSimilarCasesAsync(adminQuery);

                // Build enhanced context
                var contextualData = BuildEnhancedContext(adminQuery, claims, relevantCases);

                var prompt = $@"
                You are an expert fraud analyst with access to historical fraud patterns and current claim data.

                ADMIN QUERY: {adminQuery}

                HISTORICAL FRAUD PATTERNS FROM KNOWLEDGE BASE:
                {FormatHistoricalCases(relevantCases)}

                CURRENT CLAIMS DATA:
                {FormatCurrentClaims(claims.Take(10))} // Limit for context

                TASK:
                Provide a comprehensive analysis that:
                1. References specific historical fraud patterns from the knowledge base
                2. Identifies similar patterns in current claims
                3. Highlights emerging trends or anomalies
                4. Provides actionable insights and recommendations
                5. Quantifies risk levels where possible

                Format your response in clear sections with specific examples and evidence.
                ";

                var result = await _kernel.InvokePromptAsync(prompt);
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting contextual analysis for: {adminQuery}");
                throw;
            }
        }

        public async Task<List<RAGSearchResult>> FindSimilarFraudPatternsAsync(
            string? merchant = null, 
            double? amount = null, 
            string? serviceType = null,
            string? fraudTemplate = null)
        {
            try
            {
                var queryParts = new List<string>();

                if (!string.IsNullOrEmpty(merchant))
                    queryParts.Add($"merchant: {merchant}");
                
                if (amount.HasValue)
                    queryParts.Add($"amount around ${amount:F2}");
                
                if (!string.IsNullOrEmpty(serviceType))
                    queryParts.Add($"service type: {serviceType}");
                
                if (!string.IsNullOrEmpty(fraudTemplate))
                    queryParts.Add($"fraud pattern: {fraudTemplate}");

                var query = string.Join(" AND ", queryParts);
                
                if (string.IsNullOrEmpty(query))
                    query = "fraud patterns suspicious claims";

                return await SearchSimilarCasesAsync(query, 10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar fraud patterns");
                return new List<RAGSearchResult>();
            }
        }

        public async Task<string> GetFraudTrendsInsightsAsync(string query)
        {
            try
            {
                var trendResults = await SearchSimilarCasesAsync($"trends patterns analysis {query}", 15);

                var prompt = $@"
                Based on the following historical fraud cases, provide insights about fraud trends:

                HISTORICAL FRAUD DATA:
                {FormatHistoricalCases(trendResults)}

                ANALYSIS REQUEST: {query}

                Please provide:
                1. Key fraud trends identified
                2. Emerging patterns or new fraud schemes
                3. Risk assessment and severity levels
                4. Recommendations for prevention and detection
                5. Statistical insights where available

                Format as a comprehensive fraud trends report.
                ";

                var result = await _kernel.InvokePromptAsync(prompt);
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting fraud trends insights: {query}");
                throw;
            }
        }

        public async Task RebuildKnowledgeBaseAsync()
        {
            try
            {
                _logger.LogInformation("Rebuilding RAG Knowledge Base...");
                
                // Clear local knowledge base
                _localKnowledgeBase.Clear();
                
                // Re-initialize
                await InitializeKnowledgeBaseAsync();
                
                _logger.LogInformation("RAG Knowledge Base rebuild completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding RAG Knowledge Base");
                throw;
            }
        }

        #region Private Helper Methods

        private double CalculateRelevance(Citation result)
        {
            // Simple relevance calculation - you can enhance this based on your needs
            var partitionCount = result.Partitions.Count();
            return partitionCount > 0 ? 0.8 : 0.5; // Simplified relevance scoring
        }

        private Dictionary<string, object> ExtractMetadata(Citation result)
        {
            var metadata = new Dictionary<string, object>();
            
            // Extract metadata from partitions if available
            var firstPartition = result.Partitions.FirstOrDefault();
            if (firstPartition?.Tags != null)
            {
                foreach (var tag in firstPartition.Tags)
                {
                    metadata[tag.Key] = string.Join(", ", tag.Value);
                }
            }
            
            return metadata;
        }

        private FraudKnowledgeEntry CreateFraudKnowledgeEntry(Claim claim)
        {
            return new FraudKnowledgeEntry
            {
                ClaimId = claim.ClaimId ?? "",
                FraudTemplate = claim.FraudTemplate ?? "",
                Merchant = claim.Merchant ?? "",
                ServiceType = claim.ServiceType ?? "",
                Amount = claim.Amount,
                Location = claim.Location ?? "",
                Items = claim.Items ?? new List<string>(),
                Pattern = claim.Flags ?? "",
                RiskFactors = DetermineRiskFactors(claim),
                ContextualData = BuildContextualData(claim),
                IPAddress = claim.IPAddress ?? "",
                ReceiptHash = claim.ReceiptHash ?? "",
                UserId = claim.UserId ?? "",
                SubmissionDate = claim.SubmissionDate,
                DateOfService = claim.DateOfService
            };
        }

        private string BuildDocumentContent(FraudKnowledgeEntry entry)
        {
            return $@"
FRAUD CASE ANALYSIS
==================
Claim ID: {entry.ClaimId}
Fraud Template: {entry.FraudTemplate}
Merchant: {entry.Merchant}
Service Type: {entry.ServiceType}
Amount: ${entry.Amount:F2}
Location: {entry.Location}
Items: {string.Join(", ", entry.Items)}
Pattern Flags: {entry.Pattern}
Risk Factors: {entry.RiskFactors}
IP Address: {entry.IPAddress}
Receipt Hash: {entry.ReceiptHash}
User ID: {entry.UserId}
Service Date: {entry.DateOfService:yyyy-MM-dd}
Submission Date: {entry.SubmissionDate:yyyy-MM-dd}

CONTEXTUAL DATA:
{entry.ContextualData}

FRAUD INDICATORS:
This case represents a confirmed fraud instance with the pattern '{entry.FraudTemplate}'.
The fraudulent behavior involved {entry.RiskFactors}.
Location analysis shows {entry.Location} as the service location.
Time analysis indicates submission on {entry.SubmissionDate:yyyy-MM-dd} for services on {entry.DateOfService:yyyy-MM-dd}.
";
        }

        private string DetermineRiskFactors(Claim claim)
        {
            var factors = new List<string>();

            if (claim.FraudTemplate == "SharedReceiptAcrossUsers")
                factors.Add("Receipt sharing across multiple users");

            if (claim.Amount % 25 == 0 || claim.Amount % 50 == 0)
                factors.Add("Round amount pattern");

            if (!string.IsNullOrEmpty(claim.ReceiptHash))
                factors.Add("Duplicate receipt hash detected");

            if (claim.SubmissionDate.Date == claim.DateOfService.Date)
                factors.Add("Same-day submission");

            return string.Join("; ", factors);
        }

        private string BuildContextualData(Claim claim)
        {
            return $@"
This fraud case occurred at {claim.Merchant} in {claim.Location} for {claim.ServiceType} services.
The claim amount was ${claim.Amount:F2} submitted by user {claim.UserId}.
Items claimed: {string.Join(", ", claim.Items ?? new List<string>())}.
The fraud was categorized under the template: {claim.FraudTemplate}.
Additional flags: {claim.Flags}.
Geographic context: Services in {claim.Location} with potential IP tracking at {claim.IPAddress}.
";
        }

        private string BuildSearchQuery(Claim claim)
        {
            var queryParts = new List<string>
            {
                claim.Merchant ?? "",
                claim.ServiceType ?? "",
                $"${claim.Amount:F0}",
                claim.Location ?? ""
            };

            return string.Join(" ", queryParts.Where(p => !string.IsNullOrEmpty(p)));
        }

        private string BuildContextualInformation(List<RAGSearchResult> similarCases)
        {
            if (!similarCases.Any())
                return "No similar fraud cases found in knowledge base.";

            var context = new System.Text.StringBuilder();
            context.AppendLine("SIMILAR HISTORICAL FRAUD CASES:");
            context.AppendLine(new string('=', 40));

            foreach (var case_ in similarCases.Take(3))
            {
                context.AppendLine($"Relevance: {case_.Relevance:F2}");
                context.AppendLine($"Content: {case_.Content}");
                context.AppendLine(new string('-', 20));
            }

            return context.ToString();
        }

        private string BuildAnalysisPrompt(Claim claim, string contextualInfo)
        {
            return $@"
You are an expert fraud analyst reviewing a new HSA claim with access to historical fraud patterns.

NEW CLAIM TO ANALYZE:
====================
Claim ID: {claim.ClaimId}
Merchant: {claim.Merchant}
Service Type: {claim.ServiceType}
Amount: ${claim.Amount:F2}
Location: {claim.Location}
Items: {string.Join(", ", claim.Items ?? new List<string>())}
User: {claim.UserId}
Service Date: {claim.DateOfService:yyyy-MM-dd}
Submission Date: {claim.SubmissionDate:yyyy-MM-dd}
IP Address: {claim.IPAddress}
Receipt Hash: {claim.ReceiptHash}

HISTORICAL CONTEXT FROM KNOWLEDGE BASE:
{contextualInfo}

ANALYSIS TASK:
Based on the historical fraud patterns, analyze this new claim for potential fraud indicators.

Provide:
1. Fraud likelihood assessment (High/Medium/Low) with reasoning
2. Specific similarities to historical fraud cases
3. Red flags identified
4. Recommended investigation steps
5. Confidence level in your assessment

Format your response clearly with evidence from the historical cases.
";
        }

        private List<string> IdentifyRiskFactors(Claim claim, List<RAGSearchResult> similarCases)
        {
            var riskFactors = new List<string>();

            // Check for round amounts
            if (claim.Amount % 25 == 0 || claim.Amount % 50 == 0)
                riskFactors.Add("Round amount pattern");

            // Check for same-day submission
            if (claim.SubmissionDate.Date == claim.DateOfService.Date)
                riskFactors.Add("Same-day submission");

            // Check if similar to known fraud patterns
            if (similarCases.Any(c => c.Relevance > 0.8))
                riskFactors.Add("High similarity to known fraud cases");

            // Check for merchant risk based on similar cases
            var merchantCases = similarCases.Count(c => c.Content.Contains(claim.Merchant ?? ""));
            if (merchantCases > 1)
                riskFactors.Add($"Merchant appears in {merchantCases} previous fraud cases");

            return riskFactors;
        }

        private string GenerateRecommendedActions(Claim claim, List<RAGSearchResult> similarCases, List<string> riskFactors)
        {
            var actions = new List<string>();

            if (riskFactors.Contains("Round amount pattern"))
                actions.Add("Verify receipt authenticity and item details");

            if (riskFactors.Contains("Same-day submission"))
                actions.Add("Review submission timeline and user behavior");

            if (similarCases.Any(c => c.Relevance > 0.9))
                actions.Add("Conduct detailed comparison with similar historical fraud cases");

            if (!string.IsNullOrEmpty(claim.ReceiptHash))
                actions.Add($"Cross-reference receipt hash {claim.ReceiptHash} across all users");

            if (actions.Count == 0)
                actions.Add("Standard verification process");

            return string.Join("; ", actions);
        }

        private double CalculateConfidenceScore(List<RAGSearchResult> similarCases)
        {
            if (!similarCases.Any())
                return 0.5; // Low confidence with no historical context

            var avgRelevance = similarCases.Average(c => c.Relevance);
            var caseCount = similarCases.Count;

            // Higher confidence with more relevant historical cases
            return Math.Min(0.95, avgRelevance * 0.7 + (caseCount * 0.05));
        }

        private string BuildEnhancedContext(string query, List<Claim> currentClaims, List<RAGSearchResult> historicalCases)
        {
            return $@"
Query Context: {query}
Current Claims Count: {currentClaims.Count}
Historical Cases Found: {historicalCases.Count}
Analysis Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
";
        }

        private string FormatHistoricalCases(List<RAGSearchResult> cases)
        {
            if (!cases.Any())
                return "No relevant historical cases found.";

            var formatted = new System.Text.StringBuilder();
            foreach (var case_ in cases.Take(5))
            {
                formatted.AppendLine($"[Relevance: {case_.Relevance:F2}]");
                formatted.AppendLine(case_.Content);
                formatted.AppendLine(new string('-', 30));
            }
            return formatted.ToString();
        }

        private string FormatCurrentClaims(IEnumerable<Claim> claims)
        {
            var formatted = new System.Text.StringBuilder();
            foreach (var claim in claims)
            {
                formatted.AppendLine($"ID: {claim.ClaimId}, Merchant: {claim.Merchant}, Amount: ${claim.Amount:F2}, Date: {claim.SubmissionDate:yyyy-MM-dd}");
            }
            return formatted.ToString();
        }

        #endregion
    }
}