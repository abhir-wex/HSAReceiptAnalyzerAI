using HSAReceiptAnalyzer.Models;

namespace HSAReceiptAnalyzer.Services.Interface
{
    public interface IRAGService
    {
        /// <summary>
        /// Initialize and build the fraud knowledge base from existing claims
        /// </summary>
        Task InitializeKnowledgeBaseAsync();

        /// <summary>
        /// Index a new fraud case into the knowledge base
        /// </summary>
        Task IndexFraudCaseAsync(Claim claim);

        /// <summary>
        /// Search for similar fraud cases based on a query
        /// </summary>
        Task<List<RAGSearchResult>> SearchSimilarCasesAsync(string query, int maxResults = 5);

        /// <summary>
        /// Analyze a new claim using RAG-enhanced context
        /// </summary>
        Task<RAGAnalysisResult> AnalyzeClaimWithRAGAsync(Claim claim, string? customPrompt = null);

        /// <summary>
        /// Get contextual analysis for admin queries
        /// </summary>
        Task<string> GetContextualAnalysisAsync(string adminQuery, List<Claim> claims);

        /// <summary>
        /// Find similar fraud patterns based on specific criteria
        /// </summary>
        Task<List<RAGSearchResult>> FindSimilarFraudPatternsAsync(
            string? merchant = null, 
            double? amount = null, 
            string? serviceType = null,
            string? fraudTemplate = null);

        /// <summary>
        /// Get fraud trends and insights from the knowledge base
        /// </summary>
        Task<string> GetFraudTrendsInsightsAsync(string query);

        /// <summary>
        /// Clear and rebuild the knowledge base
        /// </summary>
        Task RebuildKnowledgeBaseAsync();
    }
}