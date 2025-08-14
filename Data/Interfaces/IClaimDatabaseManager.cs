using HSAReceiptAnalyzer.Models;
using Microsoft.Data.Sqlite;

namespace HSAReceiptAnalyzer.Data.Interfaces
{
    public interface IClaimDatabaseManager
    {
        void InitializeDatabase();
        List<Claim> GetClaims(string UserId);
        List<Claim> GetAllClaims();
        void InsertClaims(List<Claim> claims);
        void InsertClaim(Claim claim);
    }
}
