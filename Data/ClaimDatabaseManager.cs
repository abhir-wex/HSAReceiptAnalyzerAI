using HSAReceiptAnalyzer.Data.Interfaces;
using HSAReceiptAnalyzer.Models;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace HSAReceiptAnalyzer.Data
{
    public class ClaimDatabaseManager : IClaimDatabaseManager
    {
        private readonly string _dbFilePath;
        private readonly string _jsonFilePath;
        private readonly SqliteConnection _connection;

        public ClaimDatabaseManager(string dbFilePath, string jsonFilePath, string connection)
        {
            _dbFilePath = dbFilePath;
            _jsonFilePath = jsonFilePath;
            _connection = new SqliteConnection($"Data Source={_dbFilePath}"); ;
        }

        public void InitializeDatabase()
        {
            if (File.Exists(_dbFilePath))
                File.Delete(_dbFilePath);
                 _connection.Open();
                CreateTable();

            var claims = LoadClaimsFromJson();
            InsertClaims(claims);
            PrintSuspiciousClaims(_connection);
        }

        private void CreateTable()
        {
            _connection.Open();
            string query = @"
            CREATE TABLE Claims (
                ClaimId TEXT PRIMARY KEY,
                UserId TEXT,
                Name TEXT,
                Address TEXT,
                Merchant TEXT,
                ServiceType TEXT,
                Amount REAL,
                DateOfService TEXT,
                SubmissionDate TEXT,
                UserAge INTEGER,
                UserGender TEXT,
                Description TEXT,
                IsFraudulent INTEGER,
                FraudTemplate TEXT,
                Flags TEXT,
                ReceiptId TEXT,
                Category TEXT,
                ClaimLocation TEXT,
                Items TEXT,
                IPAddress TEXT
            );";

            using var cmd = new SqliteCommand(query, _connection);
            cmd.ExecuteNonQuery();
        }

        private List<Claim> LoadClaimsFromJson()
        {
            string json = File.ReadAllText(_jsonFilePath);
            return JsonSerializer.Deserialize<List<Claim>>(json);
        }

        public void InsertClaims(List<Claim> claims)
        {
            _connection.Open();
            foreach (var c in claims)
            {
                var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO Claims (
                    ClaimId, UserId, Name, Address, Merchant, ServiceType,
                    Amount, DateOfService, SubmissionDate, UserAge, UserGender,
                    Description, IsFraudulent, FraudTemplate, Flags,
                    ReceiptId, Category, ClaimLocation, Items, IPAddress
                ) VALUES (
                    @ClaimId, @UserId, @Name, @Address, @Merchant, @ServiceType,
                    @Amount, @DateOfService, @SubmissionDate, @UserAge, @UserGender,
                    @Description, @IsFraudulent, @FraudTemplate, @Flags,
                    @ReceiptId, @Category, @ClaimLocation, @Items, @IPAddress
                );";

                cmd.Parameters.AddWithValue("@ClaimId", c.ClaimId);
                cmd.Parameters.AddWithValue("@UserId", c.UserId);
                cmd.Parameters.AddWithValue("@Name", c.Name);
                cmd.Parameters.AddWithValue("@Address", c.Address);
                cmd.Parameters.AddWithValue("@Merchant", c.Merchant);
                cmd.Parameters.AddWithValue("@ServiceType", c.ServiceType);
                cmd.Parameters.AddWithValue("@Amount", c.Amount);
                cmd.Parameters.AddWithValue("@DateOfService", c.DateOfService.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@SubmissionDate", c.SubmissionDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@UserAge", c.UserAge);
                cmd.Parameters.AddWithValue("@UserGender", c.UserGender);
                cmd.Parameters.AddWithValue("@Description", c.Description);
                cmd.Parameters.AddWithValue("@IsFraudulent", c.IsFraudulent);
                cmd.Parameters.AddWithValue("@FraudTemplate", (object?)c.FraudTemplate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Flags", (object?)c.Flags ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReceiptId", (object?)c.ReceiptId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", (object?)c.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ClaimLocation", (object?)c.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Items", c.Items != null ? System.Text.Json.JsonSerializer.Serialize(c.Items) : DBNull.Value);
                cmd.Parameters.AddWithValue("@IPAddress", (object?)c.IPAddress ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        public async void InsertClaim(Claim claim)
        {
            _connection.Open();
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Claims (
                    ClaimId, UserId, Name, Address, Merchant, ServiceType,
                    Amount, DateOfService, SubmissionDate, UserAge, UserGender,
                    Description, IsFraudulent, FraudTemplate, Flags,
                    ReceiptId, Category, ClaimLocation, Items, IPAddress
                ) VALUES (
                    @ClaimId, @UserId, @Name, @Address, @Merchant, @ServiceType,
                    @Amount, @DateOfService, @SubmissionDate, @UserAge, @UserGender,
                    @Description, @IsFraudulent, @FraudTemplate, @Flags,
                    @ReceiptId, @Category, @ClaimLocation, @Items, @IPAddress
                );";

            cmd.Parameters.AddWithValue("@ClaimId", claim.ClaimId);
            cmd.Parameters.AddWithValue("@UserId", claim.UserId);
            cmd.Parameters.AddWithValue("@Name", claim.Name);
            cmd.Parameters.AddWithValue("@Address", claim.Address);
            cmd.Parameters.AddWithValue("@Merchant", claim.Merchant);
            cmd.Parameters.AddWithValue("@ServiceType", claim.ServiceType);
            cmd.Parameters.AddWithValue("@Amount", claim.Amount);
            cmd.Parameters.AddWithValue("@DateOfService", claim.DateOfService.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@SubmissionDate", claim.SubmissionDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@UserAge", claim.UserAge);
            cmd.Parameters.AddWithValue("@UserGender", claim.UserGender);
            cmd.Parameters.AddWithValue("@Description", claim.Description);
            cmd.Parameters.AddWithValue("@IsFraudulent", claim.IsFraudulent);
            cmd.Parameters.AddWithValue("@FraudTemplate", (object?)claim.FraudTemplate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Flags", (object?)claim.Flags ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptId", (object?)claim.ReceiptId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Category", (object?)claim.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ClaimLocation", (object?)claim.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Items", claim.Items != null ? System.Text.Json.JsonSerializer.Serialize(claim.Items) : DBNull.Value);
            cmd.Parameters.AddWithValue("@IPAddress", (object?)claim.IPAddress ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }


        public List<Claim> GetClaims(string UserId)
        {
            _connection.Open();
            List<Claim> claims = new List<Claim>();
            string query = @"
                SELECT 
                    ClaimId, UserId, Name, Address, Merchant, ServiceType, Amount, 
                    DateOfService, SubmissionDate, UserAge, UserGender, Description, 
                    IsFraudulent, FraudTemplate, Flags, ReceiptId, Category, Location, Items, IPAddress
                FROM Claims where UserId = @UserId;";

            using var cmd = new SqliteCommand(query, _connection);
            cmd.Parameters.AddWithValue("@UserId", UserId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var claim = new Claim
                {
                    ClaimId = reader["ClaimId"] as string,
                    UserId = reader["UserId"] as string,
                    Name = reader["Name"] as string,
                    Address = reader["Address"] as string,
                    Merchant = reader["Merchant"] as string,
                    ServiceType = reader["ServiceType"] as string,
                    Amount = (double)reader.GetDecimal(reader.GetOrdinal("Amount")),
                    DateOfService = DateTime.Parse(reader["DateOfService"] as string ?? string.Empty),
                    SubmissionDate = DateTime.Parse(reader["SubmissionDate"] as string ?? string.Empty),
                    UserAge = reader.GetInt32(reader.GetOrdinal("UserAge")),
                    UserGender = reader["UserGender"] as string,
                    Description = reader["Description"] as string,
                    IsFraudulent = reader.GetInt32(reader.GetOrdinal("IsFraudulent")),
                    FraudTemplate = reader["FraudTemplate"] == DBNull.Value ? null : reader["FraudTemplate"] as string,
                    Flags = reader["Flags"] == DBNull.Value ? null : reader["Flags"] as string,
                    ReceiptId = reader["ReceiptId"] == DBNull.Value ? null : reader["ReceiptId"] as string,
                    Category = reader["Category"] == DBNull.Value ? null : reader["Category"] as string,
                    Location = reader["ClaimLocation"] == DBNull.Value ? null : reader["ClaimLocation"] as string,
                    IPAddress = reader["IPAddress"] == DBNull.Value ? null : reader["IPAddress"] as string,
                    Items = reader["Items"] == DBNull.Value ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(reader["Items"] as string ?? "[]")
                };
                claims.Add(claim);
            }
            return claims;
        }

        public List<Claim> GetAllClaims()
        {
            var claims = new List<Claim>();

            _connection.Open();

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Claims";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var claim = new Claim
                {
                    ClaimId = reader["ClaimId"].ToString(),
                    ReceiptId = reader["ReceiptId"].ToString(),
                    UserId = reader["UserId"].ToString(),
                    Name = reader["Name"].ToString(),
                    Address = reader["Address"].ToString(),
                    Merchant = reader["Merchant"].ToString(),
                    ServiceType = reader["ServiceType"].ToString(),
                    Amount = (double)Convert.ToDecimal(reader["Amount"]),
                    DateOfService = DateTime.Parse(reader["DateOfService"].ToString()),
                    SubmissionDate = DateTime.Parse(reader["SubmissionDate"].ToString()),
                    Category = reader["Category"].ToString(),
                    Location = reader["ClaimLocation"].ToString(),
                    UserAge = Convert.ToInt32(reader["UserAge"]),
                    Items = JsonSerializer.Deserialize<List<string>>(reader["Items"].ToString()),
                    UserGender = reader["UserGender"].ToString(),
                    Description = reader["Description"].ToString(),
                    IsFraudulent = Convert.ToInt32(reader["IsFraudulent"]),
                    FraudTemplate = reader["FraudTemplate"].ToString(),
                    Flags = reader["Flags"].ToString(),
                    IPAddress = reader["IPAddress"].ToString()
                };

                claims.Add(claim);
            }

            return claims;
        }

        // Add this method to ClaimDatabaseManager
        public void CreateClaimFeaturesTable()
        {
            _connection.Open();

            var command = _connection.CreateCommand();
            command.CommandText = @"
        CREATE TABLE IF NOT EXISTS ClaimFeatures (
            ClaimId TEXT PRIMARY KEY,
            Amount REAL,
            DaysSinceLastClaim REAL,
            SubmissionDelayDays REAL,
            VendorFrequency REAL,
            CategoryFrequency REAL,
            AverageClaimAmountForUser REAL,
            AmountDeviationFromAverage REAL,
            IPAddressChangeFrequency REAL,
            ItemCount REAL,
            DistinctItemsRatio REAL,
            UserAge REAL
        )";
            command.ExecuteNonQuery();
        }
        private void PrintSuspiciousClaims(SqliteConnection conn)
        {
            Console.WriteLine("\n🔍 Suspicious Claims (Flagged as Fraudulent):\n");

            string query = "SELECT ClaimId, FraudTemplate FROM Claims WHERE IsFraudulent = 1;";
            using var cmd = new SqliteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine($"⚠️ {reader["ClaimId"]} | Template: {reader["FraudTemplate"]}");
            }
        }
    }
}