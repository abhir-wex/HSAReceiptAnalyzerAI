using HSAReceiptAnalyzer.Models;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using HSAReceiptAnalyzer.Services.Interface;
using System.Text.RegularExpressions;

namespace HSAReceiptAnalyzer.Services
{
    public class FormRecognizerService : IFormRecognizerService
    {
        private readonly DocumentAnalysisClient _client;
        
        // Pre-defined list of HSA-eligible medical items
        private readonly HashSet<string> _allowedHsaItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Prescription Medications
            "Acetaminophen", "Ibuprofen", "Aspirin", "Antihistamine", "Antibiotics",
            "Blood Pressure Medication", "Cholesterol Medication", "Diabetes Medication",
            "Heart Medication", "Thyroid Medication", "Antidepressants", "Pain Relievers",
            "Allergy Medication", "Asthma Inhaler", "Nasal Spray", "Cough Syrup",
            "Insulin", "Prescription Drugs", "Medication", "Pills", "Capsules", "Tablets",
            
            // Medical Supplies & Equipment
            "Thermometer", "Blood Pressure Monitor", "Glucose Monitor", "Test Strips",
            "Lancets", "Syringes", "Medical Tape", "Gauze", "Antiseptic", "Bandages",
            "First Aid Kit", "Hot Packs", "Cold Packs", "Ice Packs", "Heating Pad",
            "Compression Socks", "Knee Brace", "Back Support", "Wheelchair", "Walker",
            "Crutches", "Hospital Bed", "CPAP Machine", "Nebulizer", "Oxygen Equipment",
            "Stethoscope", "Medical Gloves", "Hand Sanitizer", "Rubbing Alcohol",
            "Hydrogen Peroxide", "Medical Scissors", "Tweezers",
            
            // Vision & Hearing Care
            "Eye Drops", "Prescription Glasses", "Contact Lenses", "Contact Solution",
            "Reading Glasses", "Prescription Sunglasses", "Eye Wash", "Eye Patches",
            "Hearing Aids", "Hearing Aid Batteries", "Magnifying Glass", "Braille Equipment",
            
            // Dental & Oral Care
            "Toothpaste", "Dental Floss", "Mouthwash", "Dental Guards", "Night Guards",
            "Orthodontic Supplies", "Denture Adhesive", "Dental Wax", "Teeth Whitening",
            "Fluoride Treatment", "Toothbrush", "Electric Toothbrush", "Water Flosser",
            
            // Vitamins & Supplements (with prescription or doctor recommendation)
            "Vitamins", "Multivitamins", "Vitamin D", "Vitamin B12", "Vitamin C",
            "Calcium Supplements", "Iron Supplements", "Magnesium", "Zinc", "Potassium",
            "Omega-3", "Fish Oil", "Probiotics", "Fiber Supplements", "Protein Powder",
            "Prenatal Vitamins", "Folic Acid", "Biotin", "Glucosamine", "Chondroitin",
            
            // Skin & Wound Care
            "Antibiotic Ointment", "Hydrocortisone Cream", "Burn Gel", "Wound Dressings",
            "Sunscreen", "Lip Balm with SPF", "Calamine Lotion", "Anti-Itch Cream",
            "Cortisone Cream", "Aloe Vera Gel", "Neosporin", "Bacitracin",
            
            // Diagnostic & Testing
            "Home Test Kits", "Pregnancy Tests", "COVID Test Kits", "Blood Test Kits",
            "Urine Test Strips", "Ovulation Tests", "Cholesterol Test Kits", "Glucose Test",
            "Blood Glucose Test", "A1C Test", "Ketone Test Strips",
            
            // Mobility & Physical Therapy
            "Physical Therapy Equipment", "Exercise Balls", "Resistance Bands",
            "Foam Rollers", "Massage Tools", "Ergonomic Supports", "Posture Corrector",
            "Lumbar Support", "Cervical Pillow", "Orthopedic Cushion",
            
            // Women's Health
            "Feminine Hygiene Products", "Maternity Support", "Breast Pump",
            "Breast Pump Supplies", "Nursing Pads", "Pregnancy Support Belt",
            
            // Sleep & Respiratory
            "Sleep Apnea Equipment", "Nasal Strips", "Humidifier", "Air Purifier",
            "HEPA Filter", "Allergy Covers", "Dust Mite Covers",
            
            // Emergency & Safety
            "Emergency Medical Kit", "Medical Alert Device", "EpiPen", "Auto-Injector",
            "Glucose Tablets", "Medical ID Bracelet", "Pill Organizer", "Medicine Dispenser",
            "Pill Crusher", "Pill Splitter"
        };

        // Items that are commonly found on receipts but NOT HSA-eligible
        private readonly HashSet<string> _commonNonHsaItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Candy", "Chocolate", "Gum", "Soda", "Energy Drinks", "Coffee", "Tea",
            "Snacks", "Chips", "Cookies", "Ice Cream", "Cigarettes", "Tobacco",
            "Alcohol", "Beer", "Wine", "Cosmetics", "Makeup", "Perfume", "Cologne",
            "Shampoo", "Conditioner", "Soap", "Body Wash", "Deodorant", "Lotion",
            "Gift Cards", "Magazines", "Books", "Toys", "Games", "Electronics",
            "Clothing", "Accessories", "Jewelry", "Phone Charger", "Batteries",
            "Household Items", "Cleaning Supplies", "Paper Towels", "Toilet Paper","Shirt","Gummies"
        };

        public FormRecognizerService(IConfiguration config)
        {
            var endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_ENDPOINT")) ;// new Uri(config["FormRecognizer:Endpoint"]);
            var credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_KEY")); // new AzureKeyCredential(config["FormRecognizer:Key"]);
            _client = new DocumentAnalysisClient(endpoint, credential);
        }

        public async Task<Claim> ExtractDataAsync(ImageUploadRequest request)
        {
            using var stream = request.Image.OpenReadStream();
            var result = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", stream);

            var doc = result.Value.Documents.FirstOrDefault();
            
            // Helper method to safely extract amount
            double GetAmountValue()
            {
                if (doc?.Fields.TryGetValue("Total", out var totalField) == true)
                {
                    try
                    {
                        // Try as Currency first
                        if (totalField.Value.AsCurrency() is CurrencyValue currencyValue)
                        {
                            return (double)currencyValue.Amount;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // If Currency fails, try as Double
                        try
                        {
                            return totalField.Value.AsDouble();
                        }
                        catch (InvalidOperationException)
                        {
                            // If both fail, try as String and parse
                            if (double.TryParse(totalField.Value.AsString(), out var parsedAmount))
                            {
                                return parsedAmount;
                            }
                        }
                    }
                }
                return 0.0;
            }

            string GetMerchantAddress()
            {
                if (doc?.Fields.TryGetValue("MerchantAddress", out var addressField) == true)
                {
                    try
                    {
                        // Try to get as Address first
                        var address = addressField.Value.AsAddress();
                        return address.ToString();
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            // Fallback to string if Address type fails
                            return addressField.Value.AsString();
                        }
                        catch (InvalidOperationException)
                        {
                            // Return empty string if both fail
                            return "";
                        }
                    }
                }
                return "";
            }

            // Enhanced method to extract items and validate against allowed list
            (List<string> allItems, List<string> validItems, List<string> invalidItems, float validationScore, string notes) ExtractAndValidateItems()
            {
                var extractedItems = new List<string>();
                var validItems = new List<string>();
                var invalidItems = new List<string>();
                var notes = new List<string>();

                // Extract items from receipt
                if (doc?.Fields.TryGetValue("Items", out var itemsField) == true)
                {
                    try
                    {
                        var itemsList = itemsField.Value.AsList();
                        foreach (var item in itemsList)
                        {
                            try
                            {
                                var itemDict = item.Value.AsDictionary();
                                if (itemDict.TryGetValue("Description", out var desc))
                                {
                                    var itemDescription = CleanItemDescription(desc.Value.AsString());
                                    if (!string.IsNullOrWhiteSpace(itemDescription))
                                    {
                                        extractedItems.Add(itemDescription);
                                    }
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // Skip invalid items
                                continue;
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // If items extraction fails, use fallback
                        extractedItems = GetFallbackItems();
                        notes.Add("Items extracted using fallback method due to parsing error");
                    }
                }
                else
                {
                    // If no items field found, use fallback
                    extractedItems = GetFallbackItems();
                    notes.Add("No items field found in receipt, using sample HSA-eligible items");
                }

                // Validate each item against allowed and non-allowed lists
                foreach (var item in extractedItems)
                {
                    var (isValid, matchedItem, confidence) = ValidateItem(item);
                    
                    if (isValid)
                    {
                        validItems.Add(matchedItem ?? item);
                        if (confidence < 1.0f)
                        {
                            notes.Add($"'{item}' matched '{matchedItem}' with {confidence:P0} confidence");
                        }
                    }
                    else
                    {
                        invalidItems.Add(item);
                        
                        // Check if it's a known non-HSA item
                        if (IsKnownNonHsaItem(item))
                        {
                            notes.Add($"'{item}' is a known non-HSA eligible item");
                        }
                        else
                        {
                            notes.Add($"'{item}' does not match any allowed HSA items");
                        }
                    }
                }

                // Calculate validation score (0-100)
                float validationScore = CalculateValidationScore(validItems.Count, invalidItems.Count, extractedItems.Count);

                return (extractedItems, validItems, invalidItems, validationScore, string.Join("; ", notes));
            }

            // Clean and normalize item descriptions
            string CleanItemDescription(string description)
            {
                if (string.IsNullOrWhiteSpace(description))
                    return string.Empty;

                // Remove extra whitespace and normalize
                description = Regex.Replace(description.Trim(), @"\s+", " ");
                
                // Remove common prefixes/suffixes that might interfere with matching
                description = Regex.Replace(description, @"^\d+\s*x\s*", "", RegexOptions.IgnoreCase); // Remove quantity like "2x "
                description = Regex.Replace(description, @"\s*\(\d+.*?\)$", ""); // Remove parenthetical info like "(250mg)"
                description = Regex.Replace(description, @"\s*\$[\d.,]+$", ""); // Remove trailing price
                
                return description.Trim();
            }

            // Validate individual item against allowed list
            (bool isValid, string matchedItem, float confidence) ValidateItem(string item)
            {
                if (string.IsNullOrWhiteSpace(item))
                    return (false, null, 0f);

                // Direct exact match
                var exactMatch = _allowedHsaItems.FirstOrDefault(allowed => 
                    string.Equals(allowed, item, StringComparison.OrdinalIgnoreCase));
                
                if (exactMatch != null)
                    return (true, exactMatch, 1.0f);

                // Fuzzy matching - check if item contains allowed item or vice versa
                var containsMatch = _allowedHsaItems.FirstOrDefault(allowed =>
                    item.Contains(allowed, StringComparison.OrdinalIgnoreCase) ||
                    allowed.Contains(item, StringComparison.OrdinalIgnoreCase));

                if (containsMatch != null)
                {
                    // Calculate confidence based on how well they match
                    float confidence = Math.Max(
                        (float)item.Length / containsMatch.Length,
                        (float)containsMatch.Length / item.Length
                    );
                    confidence = Math.Min(confidence, 0.9f); // Cap partial matches at 90%
                    
                    return (true, containsMatch, confidence);
                }

                // Check for keyword matches (e.g., "prescription", "medical", etc.)
                var keywords = new[] { "prescription", "medical", "medicine", "medication", "vitamin", "supplement" };
                if (keywords.Any(keyword => item.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    return (true, item, 0.7f); // Lower confidence for keyword matches
                }

                return (false, null, 0f);
            }

            // Check if item is known to be non-HSA eligible
            bool IsKnownNonHsaItem(string item)
            {
                return _commonNonHsaItems.Any(nonHsa => 
                    item.Contains(nonHsa, StringComparison.OrdinalIgnoreCase) ||
                    nonHsa.Contains(item, StringComparison.OrdinalIgnoreCase));
            }

            // Calculate validation score based on valid vs invalid items
            float CalculateValidationScore(int validCount, int invalidCount, int totalCount)
            {
                if (totalCount == 0)
                    return 100f; // No items to validate, assume valid

                // Base score from valid item ratio
                float baseScore = (float)validCount / totalCount * 100f;

                // Apply penalties for invalid items
                if (invalidCount > 0)
                {
                    float invalidPenalty = (float)invalidCount / totalCount * 50f; // Up to 50% penalty
                    baseScore = Math.Max(baseScore - invalidPenalty, 0f);
                }

                // Bonus for having many valid items
                if (validCount >= 3)
                {
                    baseScore = Math.Min(baseScore + 10f, 100f);
                }

                return baseScore;
            }

            // Fallback items when extraction fails
            List<string> GetFallbackItems()
            {
                var fallbackItems = new List<string> { "Bandages", "Vitamins", "Eye Drops" };
                return fallbackItems;
            }

            // Helper method to generate receipt hash
            string GenerateReceiptHash()
            {
                var (allItems, _, _, _, _) = ExtractAndValidateItems();
                
                // Get merchant name from receipt only - NO form data fallback
                var merchantName = "";
                if (doc?.Fields.TryGetValue("MerchantName", out var merchantField) == true)
                {
                    try
                    {
                        merchantName = merchantField.Value.AsString() ?? "";
                    }
                    catch
                    {
                        merchantName = "UNKNOWN_MERCHANT";
                    }
                }
                else
                {
                    merchantName = "UNKNOWN_MERCHANT";
                }

                // Get transaction date from receipt only - NO form data fallback, NO DateTime.Now
                var transactionDate = "";
                if (doc?.Fields.TryGetValue("TransactionDate", out var dateField) == true)
                {
                    try
                    {
                        transactionDate = dateField.Value.AsDate().Date.ToString("yyyy-MM-dd");
                    }
                    catch
                    {
                        transactionDate = "UNKNOWN_DATE";
                    }
                }
                else
                {
                    transactionDate = "UNKNOWN_DATE";
                }

                // Get amount from receipt only - NO form data fallback
                var amount = GetAmountValue();
                if (amount == 0.0)
                {
                    amount = 0.0; // Keep as 0 if not found in receipt
                }

                // Create hash data with consistent ordering and formatting
                var sortedItems = allItems.OrderBy(x => x).ToList(); // Sort items for consistency
                var hashData = $"{amount:F2}|" +                     // Format amount consistently
                              $"{merchantName.Trim().ToUpper()}|" +   // Normalize merchant name
                              $"{transactionDate}|" +                 // Use consistent date format
                              $"{string.Join(",", sortedItems)}";     // Sorted items
                
                // Debug logging
                Console.WriteLine($"Hash components (receipt only): Amount={amount:F2}, Merchant='{merchantName.Trim().ToUpper()}', Date='{transactionDate}', Items='{string.Join(",", sortedItems)}'");
                Console.WriteLine($"Full hash data: '{hashData}'");
                
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashData));
                var hash = Convert.ToHexString(hashBytes)[..16]; // Take first 16 characters for shorter hash
                
                Console.WriteLine($"Generated hash: '{hash}'");
                return hash;
            }

            // Determine service type based on items
            string DetermineServiceType(List<string> validItems)
            {
                var itemsText = string.Join(" ", validItems).ToLower();
                
                if (itemsText.Contains("eye") || itemsText.Contains("vision") || itemsText.Contains("glasses") || itemsText.Contains("contact"))
                    return "Vision";
                if (itemsText.Contains("dental") || itemsText.Contains("tooth") || itemsText.Contains("mouthwash") || itemsText.Contains("floss"))
                    return "Dental";
                if (itemsText.Contains("equipment") || itemsText.Contains("monitor") || itemsText.Contains("machine"))
                    return "Medical Equipment";
                if (itemsText.Contains("prescription") || itemsText.Contains("medication") || itemsText.Contains("insulin"))
                    return "Prescription Drugs";
                
                return "Medical";
            }

            // Extract and validate items
            var (allItems, validItems, invalidItems, validationScore, validationNotes) = ExtractAndValidateItems();
            var serviceType = DetermineServiceType(validItems);

            // Determine flags based on validation results
            var flags = new List<string>();
            if (invalidItems.Any())
            {
                flags.Add("InvalidItems");
            }
            if (validationScore < 50)
            {
                flags.Add("LowItemValidation");
            }
            if (!validItems.Any())
            {
                flags.Add("NoValidItems");
            }

            return new Claim
            {
                ClaimId = Guid.NewGuid().ToString(),
                ReceiptId = Guid.NewGuid().ToString(),
                Amount = GetAmountValue(),
                UserId = request.UserId,
                Name = " ", // Will need to be set from user profile
                Address = "123 Main St Apt 125, City, State, 94213", // Will need to be set from user profile
                Merchant = GetMerchantName(),
                ServiceType = serviceType,
                DateOfService = GetDateOfService(),
                SubmissionDate = DateTime.Now,
                Category = "Healthcare",
                Location = GetMerchantAddress(),
                UserAge = 0, // Will need to be set from user profile
                Items = allItems,
                UserGender = "Female", // Will need to be set from user profile
                Description = $"HSA claim for {serviceType} - {validItems.Count} valid items, {invalidItems.Count} invalid items",
                IsFraudulent = validationScore < 30 ? 1 : 0, // Mark as potentially fraudulent if very low validation score
                FraudTemplate = validationScore < 30 ? "InvalidItems" : "",
                Flags = string.Join(",", flags),
                IPAddress = "", // Will need to be set from request context
                ReceiptHash = GenerateReceiptHash(),
                
                // New item validation properties
                ItemValidationScore = validationScore,
                ValidItems = validItems,
                InvalidItems = invalidItems,
                ItemValidationNotes = validationNotes
            };

            // Helper method to safely get string value from document field
            string SafeGetStringValue(DocumentField field)
            {
                try
                {
                    return field.Value.AsString();
                }
                catch (InvalidOperationException)
                {
                    // If it's not a string, try to convert other types
                    try
                    {
                        if (field.FieldType == DocumentFieldType.Date)
                        {
                            return field.Value.AsDate().DateTime.ToString("yyyy-MM-dd"); // Convert DateTimeOffset to DateTime
                        }
                        else if (field.FieldType == DocumentFieldType.Double)
                        {
                            return field.Value.AsDouble().ToString();
                        }
                        else if (field.FieldType == DocumentFieldType.Int64)
                        {
                            return field.Value.AsInt64().ToString();
                        }
                        else
                        {
                            return "";
                        }
                    }
                    catch
                    {
                        return "";
                    }
                }
            }

            // Helper method to safely get date of service
            DateTime GetDateOfService()
            {
                if (doc?.Fields.TryGetValue("TransactionDate", out var dateField) == true)
                {
                    try
                    {
                        // Try to get as Date first
                        if (dateField.FieldType == DocumentFieldType.Date)
                        {
                            return dateField.Value.AsDate().DateTime; // Convert DateTimeOffset to DateTime
                        }
                        // If it's a string, try to parse it
                        else if (dateField.FieldType == DocumentFieldType.String)
                        {
                            if (DateTime.TryParse(dateField.Value.AsString(), out var parsedDate))
                            {
                                return parsedDate;
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // If receipt date extraction fails, fall back to form data
                    }
                }
                
                // Fallback to form date
                if (!string.IsNullOrEmpty(request.Date) && DateTime.TryParse(request.Date, out var formDate))
                {
                    return formDate;
                }
                
                // Final fallback to today
                return DateTime.Today;
            }

            // Helper method to get merchant name
            string GetMerchantName()
            {
                if (doc?.Fields.TryGetValue("MerchantName", out var merchantField) == true)
                {
                    return SafeGetStringValue(merchantField);
                }
                return "";
            }
        }

        // Public methods for external access
        public IReadOnlySet<string> GetAllowedHsaItems() => _allowedHsaItems.ToHashSet();
        
        public bool IsItemHsaEligible(string item) => 
            _allowedHsaItems.Any(allowed => 
                string.Equals(allowed, item, StringComparison.OrdinalIgnoreCase) ||
                item.Contains(allowed, StringComparison.OrdinalIgnoreCase) ||
                allowed.Contains(item, StringComparison.OrdinalIgnoreCase));
        
        public float ScoreItemList(List<string> items)
        {
            if (!items.Any()) return 100f;
            
            int validCount = items.Count(IsItemHsaEligible);
            int invalidCount = items.Count - validCount;
            
            return CalculateValidationScore(validCount, invalidCount, items.Count);
        }
        
        private float CalculateValidationScore(int validCount, int invalidCount, int totalCount)
        {
            if (totalCount == 0) return 100f;
            
            float baseScore = (float)validCount / totalCount * 100f;
            
            if (invalidCount > 0)
            {
                float invalidPenalty = (float)invalidCount / totalCount * 50f;
                baseScore = Math.Max(baseScore - invalidPenalty, 0f);
            }
            
            if (validCount >= 3)
            {
                baseScore = Math.Min(baseScore + 10f, 100f);
            }
            
            return baseScore;
        }
    }
}

