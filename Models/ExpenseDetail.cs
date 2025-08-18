namespace HSAReceiptAnalyzer.Models
{
    public class ExpenseDetail
    {
        public int ClaimId { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int UserId { get; set; }
        public string Merchant { get; set; }
        public string Name { get; set; }
        public int ReceiptId { get; set; }
        public DateTime DateOfService { get; set; }
        public decimal Amount { get; set; }
        public string ServiceType { get; set; }
        public string Category { get; set; }
        public string CategoryDescription { get; set; }
        public string Location { get; set; }
        public string VendorName { get; set; }
    }
}
