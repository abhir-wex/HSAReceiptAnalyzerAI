using System.ComponentModel.DataAnnotations;

namespace HSAReceiptAnalyzer.Models
{
    public class FraudKnowledgeEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ClaimId { get; set; } = string.Empty;
        public string FraudTemplate { get; set; } = string.Empty;
        public string Merchant { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public string Pattern { get; set; } = string.Empty;
        public string RiskFactors { get; set; } = string.Empty;
        public DateTime DateIndexed { get; set; } = DateTime.UtcNow;
        public string ContextualData { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string ReceiptHash { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public DateTime DateOfService { get; set; }
    }

    public class RAGSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public string Source { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class RAGAnalysisResult
    {
        public string Query { get; set; } = string.Empty;
        public string Analysis { get; set; } = string.Empty;
        public List<RAGSearchResult> SimilarCases { get; set; } = new();
        public List<string> RiskFactors { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    }
}