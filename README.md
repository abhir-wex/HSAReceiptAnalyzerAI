# HSA Receipt Analyzer with RAG Implementation :microscope: :file_folder:

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![React](https://img.shields.io/badge/React-18.x-blue.svg)](https://reactjs.org/)
[![Azure](https://img.shields.io/badge/Azure-AI%20Services-0078d4.svg)](https://azure.microsoft.com/en-us/services/cognitive-services/)
[![WEX](https://img.shields.io/badge/Powered%20by-WEX%20Technology-orange.svg)](https://www.wexinc.com/)

A sophisticated **Healthcare Savings Account (HSA) receipt fraud detection system** that combines traditional Machine Learning with **Retrieval-Augmented Generation (RAG)** to provide intelligent, context-aware fraud analysis.

## :bulb: Overview

This system revolutionizes fraud detection by combining:
- **Traditional ML Models** (LightGBM) for pattern-based fraud detection
- **RAG Technology** for contextual analysis using historical fraud patterns
- **Azure AI Services** for receipt OCR and data extraction
- **Semantic Kernel** for intelligent fraud reasoning
- **Real-time Learning** that improves with each fraud case
- **Advanced HSA Item Validation** for comprehensive eligibility checking

## :star: Latest Enhancements (v2.0)

### :rocket: **Enhanced RAG-Powered Fraud Detection**
- **Advanced Item Validation**: Automatic detection of non-HSA eligible items (alcohol, tobacco, candy)
- **Intelligent Fraud Reasoning**: Combined ML + RAG scoring with evidence-based recommendations
- **Real-time Pattern Learning**: Automatic indexing of new fraud cases for future reference
- **Enhanced Response Structure**: Human-readable explanations with technical details
- **Graceful Degradation**: RAG-first analysis with fallback to traditional pattern detection

### :zap: **Smart Item Analysis**
- **70% Invalid Item Threshold**: Automatic fraud flagging for high ratios of non-HSA items
- **Suspicious Item Detection**: Recognition of clearly non-eligible items (alcohol, tobacco, etc.)
- **Item Validation Scoring**: Comprehensive scoring with detailed breakdowns
- **Category-based Organization**: Medications, Medical Supplies, Vision Care, Dental Care

### :chart_with_upwards_trend: **Improved Admin Experience**
- **Natural Language Queries**: "Show me fraud patterns at HealthMart with round amounts"
- **RAG-First Analysis**: Contextual intelligence with pattern-based fallback
- **HSA Item Catalog**: Searchable database of eligible items by category
- **Enhanced Debugging**: Comprehensive logging for fraud investigation

## :star: Key Features

### :zap: **Enhanced Fraud Detection**
- **Contextual Analysis**: "This $250 claim matches 3 historical fraud cases at HealthMart"
- **Evidence-Based Decisions**: Specific historical patterns referenced in analysis
- **Risk Factor Identification**: Round amounts, same-day submissions, IP anomalies, invalid items
- **Intelligent Recommendations**: "Cross-reference receipt hash across all users"

### :rocket: **RAG-Powered Intelligence**
- **Knowledge Base**: Automatically indexed fraud cases with contextual metadata
- **Semantic Search**: Find similar fraud patterns across historical data
- **Continuous Learning**: System gets smarter with each new fraud case
- **Trend Analysis**: Identify evolving fraud schemes and emerging patterns

### :bar_chart: **Comprehensive Analytics**
- **Admin Dashboard**: Natural language queries about fraud patterns
- **Fraud Trends**: "Show me round amount fraud patterns in the last 6 months"
- **Merchant Risk Analysis**: Historical fraud rates by merchant
- **Pattern Evolution**: Track how fraud schemes change over time

## :mag: Real-World RAG Use Cases

### 1. **Contextual Fraud Analysis**
- **What it does**: When analyzing a new claim, RAG searches through historical fraud cases to find similar patterns
- **Real example**: New claim for $250 at "HealthMart Pharmacy" — RAG finds 3 similar fraud cases at same merchant with round amounts
- **Business value**: Provides investigators with specific historical context instead of generic ML scores

### 2. **Expert Knowledge Retention**
- **What it does**: Captures and retains fraud investigation expertise in a searchable knowledge base
- **Real example**: Experienced fraud analyst retires — Their knowledge of specific fraud schemes remains accessible through RAG
- **Business value**: Institutional knowledge preservation and consistent fraud detection quality

### 3. **Pattern Evolution Detection**
- **What it does**: Identifies how fraud patterns evolve over time by comparing new cases to historical trends
- **Real example**: "SharedReceiptAcrossUsers" pattern evolving to include IP address manipulation
- **Business value**: Proactive fraud prevention and early detection of emerging schemes

### 4. **Intelligent Item Validation** ⭐ NEW
- **What it does**: Automatically detects non-HSA eligible items and flags suspicious receipt contents
- **Real example**: Receipt containing "Beer, Wine, Acetaminophen" — System flags alcohol items while allowing valid medication
- **Business value**: Prevents fraudulent claims for clearly ineligible items while maintaining accuracy for legitimate claims

## :triangular_flag_on_post: Enhanced Architecture

```
+---------------------+    +---------------------+    +---------------------+
|   React Frontend    |--->|  .NET 8 Web API     |--->|   Azure Services    |
|                     |    |                     |    |                     |
| - Receipt Upload    |    | - RAG Analysis      |    | - Form Recognizer   |
| - Admin Dashboard   |    | - ML Fraud Model    |    | - WEX AI Gateway    |
| - HSA Item Catalog  |    | - Item Validation   |    | - OpenAI Models     |
| - Results Display   |    | - Knowledge Base    |    | - Semantic Kernel   |
+---------------------+    +---------------------+    +---------------------+
                                      |
                                      v
                           +---------------------+
                           |   SQLite Database   |
                           |                     |
                           | - Claims Data       |
                           | - Fraud Patterns    |
                           | - HSA Item Rules    |
                           | - User History      |
                           +---------------------+
```

### Enhanced RAG Technical Architecture
```
+---------------------+
|    New Claim        |
|    Analysis         |
+----------+----------+
           |
           v
+---------------------+    +---------------------+    +---------------------+
|   Item Validation   |    |   Traditional ML    |    |    RAG Service      |
|   - HSA Eligibility |<-->|   Fraud Detection   |<-->|                     |
|   - Suspicious Items|    |   (LightGBM)        |    |  - Knowledge Base   |
|   - Ratio Analysis  |    +---------------------+    |  - Semantic Search  |
+---------------------+               |                |  - Context Analysis |
           |                          |                |  - Pattern Learning |
           |                          |                +---------------------+
           v                          v                           |
+---------------------+    +---------------------+               |
|   Enhanced Scoring  |    |   RAG Insights      |<--------------+
|   - ML Score        |<-->|   - Historical Cases|
|   - Item Score      |    |   - Risk Factors    |
|   - RAG Confidence  |    |   - Recommendations |
+---------------------+    +---------------------+
           |                          |
           v                          v
+---------------------+    +---------------------+
|   Intelligent       |    |   Fraud Case        |
|   Response          |    |   Indexing          |
|   - User-Readable   |    |   (if fraudulent)   |
|   - Technical       |    +---------------------+
|   - Evidence-Based  |
+---------------------+
```

## :rocket: Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for React frontend)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- WEX AI Gateway API Key
- Azure Form Recognizer Service (optional)

### 1. Clone the Repository

```bash
git clone https://github.com/abhir-wex/HSAReceiptAnalyzerAI.git
cd HSAReceiptAnalyzerAI
```

### 2. Backend Setup (.NET 8)

#### Configure API Keys

**Option A: Using appsettings.json (Recommended for Development)**

Update `appsettings.json` and `appsettings.Development.json`:

```json
{
  "WEXOpenAI": {
    "Endpoint": "https://aips-ai-gateway.ue1.dev.ai-platform.int.wexfabric.com/",
    "Key": "YOUR_WEX_API_KEY_HERE"
  },
  "FormRecognizer": {
    "Endpoint": "https://your-form-recognizer.cognitiveservices.azure.com/",
    "Key": "YOUR_AZURE_KEY_HERE"
  }
}
```

**Option B: Using Environment Variables**

Create a `.env` file in the project root:

```env
WEX_OPENAI_ENDPOINT=https://aips-ai-gateway.ue1.dev.ai-platform.int.wexfabric.com/
WEX_OPENAI_KEY=your_wex_api_key_here
AZURE_FORM_RECOGNIZER_ENDPOINT=your_azure_endpoint
AZURE_FORM_RECOGNIZER_KEY=your_azure_key
```

**Option C: Using User Secrets (Most Secure)**

```bash
dotnet user-secrets init
dotnet user-secrets set "WEXOpenAI:Key" "your_wex_api_key_here"
dotnet user-secrets set "FormRecognizer:Key" "your_azure_key_here"
```

#### Install Dependencies and Run

```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

The API will be available at `https://localhost:7041` or `http://localhost:5041`

### 3. Frontend Setup (React)

```bash
# Navigate to frontend directory
cd Frontend/frontend

# Install dependencies
npm install

# Start the development server
npm start
```

The React app will be available at `http://localhost:3000`

### 4. Initialize Sample Data

The system automatically initializes with sample fraud data from `Data/multiple_users.json` on first startup. Check the logs for:

```
info: Initializing RAG Knowledge Base...
info: Found X fraud cases to index
info: RAG Knowledge Base initialized successfully
```

## :link: Enhanced API Endpoints

### :rocket: RAG-Enhanced Endpoints (Primary)

#### `POST /api/RAGAnalyze/enhanced-fraud-check` ⭐ ENHANCED
**Complete RAG-enhanced fraud analysis with item validation**

```bash
curl -X POST "https://localhost:7041/api/RAGAnalyze/enhanced-fraud-check" \
  -H "Content-Type: multipart/form-data" \
  -F "image=@receipt.jpg"
```

**Enhanced Response with RAG Context & Item Validation:**
```json
{
  "claimId": "CLAIM-2025-001",
  "isFraudulent": true,
  "fraudScore": 87.5,
  "riskLevel": "High",
  "mlScore": 82.0,
  "ragConfidence": 0.89,
  "message": "⚠ FRAUD DETECTED (87.5%, High risk). This pattern matches 3 historical fraud case(s). Key factors: Round amount pattern, Invalid HSA items.",
  "userReadableText": "This receipt contains items that are not eligible for HSA reimbursement, including alcohol and tobacco products. The claim amount of $250 also matches suspicious round-amount patterns from previous fraud cases.",
  "technicalDetails": "Receipt hash analysis indicates potential SharedReceiptAcrossUsers pattern with 92% confidence based on historical data.",
  "fraudReason": "InvalidHSAItems",
  "ragAnalysis": "Based on historical patterns, this $250 claim at HealthMart matches SharedReceiptAcrossUsers template...",
  "similarHistoricalCases": [
    {
      "relevance": 0.92,
      "summary": "FRAUD CASE: $250 at HealthMart Pharmacy, SharedReceiptAcrossUsers pattern...",
      "source": "FraudKnowledgeBase"
    }
  ],
  "riskFactors": ["Round amount pattern", "Invalid HSA items", "Historical pattern match"],
  "recommendedAction": "Verify receipt authenticity and item details; Cross-reference receipt hash across all users",
  "itemValidation": {
    "score": 25.5,
    "validItems": ["Acetaminophen", "Bandages"],
    "invalidItems": ["Beer", "Wine", "Cigarettes"],
    "notes": "Contains clearly non-HSA eligible items: Beer, Wine, Cigarettes",
    "totalItems": 5,
    "validItemsCount": 2,
    "invalidItemsCount": 3,
    "invalidItemsRatio": 0.6,
    "suspiciousItems": ["Beer", "Wine", "Cigarettes"],
    "isItemValidationFraud": true
  },
  "analysisTimestamp": "2025-01-27T10:30:00Z"
}
```

#### `POST /api/RAGAnalyze/contextual-admin-analysis` ⭐ ENHANCED
**RAG-first admin analysis with pattern fallback**

```bash
curl -X POST "https://localhost:7041/api/RAGAnalyze/contextual-admin-analysis" \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me fraud patterns at HealthMart Pharmacy with round amounts"}'
```

**Enhanced Admin Query Response:**
```json
{
  "query": "Show me fraud patterns at HealthMart Pharmacy with round amounts",
  "summaryType": "RAG_Enhanced_Analysis",
  "contextualAnalysis": "## Fraud Analysis: HealthMart Pharmacy Round Amount Patterns\n\n### Historical Evidence:\n- 5 confirmed fraud cases with round amounts ($100, $250, $500)\n- 'SharedReceiptAcrossUsers' template appears in 80% of cases\n- Geographic clustering: 3 cases from Phoenix area\n- **NEW**: 60% of cases also contained invalid HSA items\n\n### Risk Assessment:\n- **HIGH RISK**: Round amounts correlate with 89% fraud rate\n- **Emerging Pattern**: Receipt hash duplication across users\n- **Item Pattern**: Invalid items found in 60% of round amount cases\n\n### Recommendations:\n1. Flag all HealthMart claims with round amounts\n2. Implement real-time receipt hash verification\n3. **NEW**: Enhanced item validation for HealthMart claims",
  "analysisMethod": "RAG_Contextual",
  "similarPatterns": [
    {
      "relevance": 0.95,
      "pattern": "Round amount pattern at HealthMart with invalid items - 5 cases detected...",
      "metadata": {"merchant": "HealthMart", "pattern": "RoundAmountWithInvalidItems"}
    }
  ],
  "totalClaimsAnalyzed": 1250,
  "fraudCasesInKnowledgeBase": 85,
  "analysisTimestamp": "2025-01-27T10:30:00Z"
}
```

#### `POST /api/RAGAnalyze/ai-analysis` ⭐ NEW
**Combined RAG and Semantic Kernel analysis**

```bash
curl -X POST "https://localhost:7041/api/RAGAnalyze/ai-analysis" \
  -H "Content-Type: multipart/form-data" \
  -F "image=@receipt.jpg"
```

#### `GET /api/RAGAnalyze/hsa-items` ⭐ NEW
**Retrieve categorized HSA-eligible items**

```bash
curl -X GET "https://localhost:7041/api/RAGAnalyze/hsa-items"
```

**HSA Items Response:**
```json
{
  "totalCount": 150,
  "items": ["Acetaminophen", "Bandages", "Blood pressure monitor", ...],
  "categories": {
    "medications": ["Acetaminophen", "Ibuprofen", "Prescription drugs", ...],
    "medicalSupplies": ["Bandages", "Gauze", "Medical thermometer", ...],
    "visionCare": ["Eye drops", "Contact solution", "Prescription glasses", ...],
    "dentalCare": ["Dental floss", "Toothbrush", "Dental visits", ...]
  },
  "lastUpdated": "2025-01-27T10:30:00Z"
}
```

#### `POST /api/RAGAnalyze/fraud-trends-analysis`
**Analyze fraud trends using historical knowledge**

```bash
curl -X POST "https://localhost:7041/api/RAGAnalyze/fraud-trends-analysis" \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Analyze fraud trends in the last 6 months"}'
```

#### `POST /api/RAGAnalyze/search-similar-cases`
**Search for similar fraud patterns**

```bash
curl -X POST "https://localhost:7041/api/RAGAnalyze/search-similar-cases" \
  -H "Content-Type: application/json" \
  -d '{
    "merchant": "HealthMart Pharmacy",
    "amount": 250.0,
    "fraudTemplate": "SharedReceiptAcrossUsers"
  }'
```

### Traditional Endpoints (Backward Compatible)

#### `POST /Analyze/fraud-check`
**Traditional ML-based fraud detection with enhanced item validation**

#### `POST /Analyze/adminAnalyze`
**Pattern-based analysis with RAG fallback**

#### `POST /Analyze/ai-analysis`
**Traditional AI analysis**

#### `GET /Analyze/hsa-items`
**HSA eligible items (legacy endpoint)**

## :chart_with_upwards_trend: Enhanced RAG vs Traditional Analysis

### Before Enhancement (Traditional):
```json
{
  "IsFraudulent": true,
  "FraudScore": 85,
  "Message": "⚠ High risk claim detected"
}
```

### After Enhancement (RAG v2.0):
```json
{
  "IsFraudulent": true,
  "FraudScore": 87.5,
  "RiskLevel": "High",
  "Message": "⚠ FRAUD DETECTED (87.5%, High risk). This pattern matches 3 historical fraud case(s). Key factors: Round amount pattern, Invalid HSA items.",
  "UserReadableText": "This receipt contains non-HSA eligible items including alcohol products. The $250 round amount also matches suspicious patterns from 3 previous fraud cases at the same merchant.",
  "TechnicalDetails": "Receipt hash indicates SharedReceiptAcrossUsers pattern. Item validation score: 25.5% (3 of 5 items invalid). RAG confidence: 89%.",
  "FraudReason": "InvalidHSAItems",
  "RAGAnalysis": "Based on historical fraud patterns, this claim shows HIGH similarity to confirmed fraud cases...",
  "SimilarHistoricalCases": [...],
  "RiskFactors": ["Round amount pattern", "Invalid HSA items", "Historical pattern match", "Same merchant fraud history"],
  "RecommendedAction": "Verify receipt authenticity and item details; Cross-reference receipt hash across all users; Review merchant risk profile",
  "ItemValidation": {
    "Score": 25.5,
    "InvalidItemsRatio": 0.6,
    "SuspiciousItems": ["Beer", "Wine"],
    "IsItemValidationFraud": true
  }
}
```

## :mag_right: Enhanced Use Case Examples

### Scenario 1: Sophisticated Item Validation Fraud ⭐ NEW

1. **New Claim Submitted**: 
   - Amount: $75.50
   - Merchant: "Corner Store Pharmacy"
   - Items: ["Acetaminophen", "Beer", "Wine", "Bandages", "Cigarettes"]

2. **Enhanced Analysis Process**:
   ```
   Item Validation: 2 valid / 5 total items (40% validation score)
   Suspicious Items Detected: Beer, Wine, Cigarettes (clearly non-HSA)
   Invalid Items Ratio: 60% (exceeds 70% threshold)
   
   RAG Search: "Corner Store invalid items fraud patterns"
   Historical Matches: 2 similar cases with alcohol/tobacco items
   ```

3. **Intelligent Investigation Result**:
   ```
   FRAUD ALERT: Invalid HSA Items Detected
   Reason: Contains clearly non-HSA eligible items: Beer, Wine, Cigarettes
   Historical Context: Matches 2 previous fraud patterns with alcohol/tobacco
   Investigation Priority: HIGH - Item-based fraud with historical precedent
   Recommended Action: Reject claim; Review merchant compliance training
   ```

### Scenario 2: Complex Pattern Recognition

1. **New Claim Submitted**: 
   - Amount: $250.00
   - Merchant: HealthMart Pharmacy
   - User: USR0501
   - Items: All valid HSA items

2. **RAG Analysis Process**:
   ```
   Search Query: "HealthMart Pharmacy $250 Medical Equipment"
   
   Knowledge Base Search Results:
   - 92% relevance: Previous fraud at HealthMart for $250 (SharedReceiptAcrossUsers)
   - 88% relevance: Round amount fraud pattern at HealthMart
   - 85% relevance: Same merchant, different user, similar timeframe
   
   Item Validation: 100% valid HSA items
   Combined Analysis: High ML score + Strong RAG match + Valid items = Sophisticated fraud
   ```

3. **Enhanced Investigation Result**:
   ```
   FRAUD ALERT: Sophisticated fraud pattern detected
   Historical Evidence: Receipt hash found in 2 previous fraud cases
   Item Analysis: All items are HSA-eligible (sophisticated fraud attempt)
   Pattern: SharedReceiptAcrossUsers with valid items to avoid detection
   Investigation Priority: CRITICAL - Sophisticated fraud requiring immediate attention
   Recommended Action: Contact HealthMart to verify transaction authenticity; Cross-reference receipt metadata
   ```

## :gear: Enhanced Configuration

### Key Configuration Sections

| Setting | Description | Required |
|---------|-------------|----------|
| `WEXOpenAI:Endpoint` | WEX AI Gateway endpoint | ✅ |
| `WEXOpenAI:Key` | WEX API key | ✅ |
| `FormRecognizer:Endpoint` | Azure Form Recognizer endpoint | ⚠️ Optional |
| `FormRecognizer:Key` | Azure Form Recognizer key | ⚠️ Optional |

### Enhanced Database Configuration

The system uses SQLite by default with enhanced schemas:
- **Database**: `ClaimsDB1.sqlite` (auto-created)
- **Sample Data**: `Data/multiple_users.json`
- **Location**: Project root directory
- **New Tables**: HSA item rules, fraud pattern cache, RAG knowledge index

### RAG System Configuration

The enhanced RAG system automatically initializes on application startup:

```csharp
// In Program.cs - Enhanced initialization
using (var scope = app.Services.CreateScope())
{
    var ragService = scope.ServiceProvider.GetRequiredService<IRAGService>();
    await ragService.InitializeKnowledgeBaseAsync(); // Indexes existing fraud cases
    await ragService.BuildItemValidationRulesAsync(); // NEW: HSA item rules
}
```

## :white_check_mark: Enhanced Testing

### 1. Test Enhanced Fraud Detection with Item Validation ⭐ NEW

1. Start both backend and frontend
2. Navigate to `http://localhost:3000`
3. Go to the "Claims" tab
4. Test different scenarios:
   - **Valid HSA claim**: Upload receipt with only medical items
   - **Invalid items fraud**: Create claim with alcohol/tobacco items
   - **Mixed items**: Test the 70% invalid threshold
   - **Round amount + invalid items**: Test combined pattern detection

### 2. Test Enhanced Admin Analysis ⭐ NEW

1. Go to the "Administrator" tab
2. Try enhanced queries:
   - "Show me fraud patterns involving alcohol or tobacco items"
   - "Analyze HealthMart claims with invalid HSA items"
   - "Find round amount patterns with item validation issues"
   - "What are the most common invalid items in fraud cases?"

### 3. Test HSA Item Catalog ⭐ NEW

1. Navigate to the HSA Items endpoint: `GET /api/RAGAnalyze/hsa-items`
2. Verify categorized item lists
3. Test item validation logic with known valid/invalid items

### 4. Test RAG Knowledge Base

Use API tools like Postman or curl to test the enhanced RAG endpoints directly.

## :floppy_disk: Enhanced Sample Data

The system includes comprehensive sample data with new validation scenarios:
- **500+ claims** across multiple users
- **Known fraud patterns**: SharedReceiptAcrossUsers, RoundAmountPattern, InvalidHSAItems
- **Multiple merchants**: HealthMart Pharmacy, PharmaPoint, MediShop, Corner Store
- **Item validation cases**: Valid medical claims, alcohol/tobacco fraud, mixed scenarios
- **Geographic distribution**: Phoenix, Chicago, Los Angeles, Houston, New York

### Enhanced RAG Knowledge Base Content

The RAG system automatically indexes:
- ✅ **Confirmed Fraud Cases** from your `multiple_users.json`
- ✅ **Fraud Templates**: SharedReceiptAcrossUsers, RoundAmountPattern, InvalidHSAItems ⭐ NEW
- ✅ **Merchant Risk Patterns**: HealthMart, PharmaPoint, MediShop cases
- ✅ **Item Validation Patterns**: Alcohol/tobacco fraud, invalid item ratios ⭐ NEW
- ✅ **Geographic Anomalies**: Location-based fraud indicators
- ✅ **Temporal Patterns**: Time-based fraud behaviors
- ✅ **IP Address Clustering**: Network-based fraud detection

## :shield: Enhanced Fraud Detection Capabilities

### Traditional ML Detection
- **Round amount patterns** ($100, $250, $500)
- **High frequency submissions** (multiple claims same day)
- **Unusual timing patterns** (late night, weekend submissions)
- **Geographic anomalies** (IP address clustering)

### Enhanced RAG Detection ⭐ v2.0
- **Historical pattern matching** with relevance scoring
- **Contextual risk assessment** based on similar cases
- **Evidence-based recommendations** with specific actions
- **Merchant risk profiling** using historical fraud rates
- **Advanced item validation** with suspicious item detection ⭐ NEW
- **Multi-factor fraud reasoning** combining ML, RAG, and item analysis ⭐ NEW
- **Pattern evolution tracking** for emerging fraud schemes ⭐ NEW

### New Fraud Detection Rules ⭐ v2.0
- **Invalid Item Ratio**: Automatic fraud flagging when ≥70% of items are non-HSA eligible
- **Suspicious Item Detection**: Recognition of clearly prohibited items (alcohol, tobacco, candy)
- **Low Validation Score**: Fraud flagging for validation scores <20%
- **Combined Pattern Analysis**: ML + RAG + Item validation for comprehensive scoring

## :trophy: Enhanced Business Impact

### Immediate Benefits:
- **Enhanced Accuracy**: Combined ML + RAG + Item validation provides superior fraud detection
- **Contextual Insights**: Investigators see specific historical patterns with item-level details
- **Faster Investigation**: Relevant historical cases and item analysis surface automatically
- **Knowledge Retention**: Fraud expertise captured and searchable with item validation rules
- **Regulatory Compliance**: Automated HSA eligibility checking reduces compliance risks ⭐ NEW

### Long-term Value:
- **Adaptive Learning**: System improves with each new fraud case and item validation rule
- **Pattern Evolution**: Detects emerging fraud schemes including sophisticated item-based fraud
- **Institutional Knowledge**: Preserves investigator expertise including item validation expertise
- **Proactive Prevention**: Trend analysis enables preventive measures for new fraud patterns
- **Cost Reduction**: Automated item validation reduces manual review costs ⭐ NEW

## :construction: Enhanced Development

### Enhanced Project Structure

```
HSAReceiptAnalyzerAI/
├── Controllers/           # API controllers
│   ├── AnalyzeController.cs       # Traditional fraud detection (enhanced)
│   ├── RAGAnalyzeController.cs    # RAG-enhanced endpoints (v2.0) ⭐
│   └── ClaimDatabaseController.cs # Database management
├── Services/             # Business logic services
│   ├── RAGService.cs              # RAG implementation (enhanced) ⭐
│   ├── FraudDetectionService.cs   # ML fraud detection (enhanced)
│   ├── SemanticKernelService.cs   # AI prompt handling
│   └── FormRecognizerService.cs   # Receipt OCR + Item validation ⭐
├── Models/               # Data models
│   ├── Claim.cs                   # Core claim model (enhanced)
│   ├── FraudKnowledgeEntry.cs     # RAG knowledge model
│   ├── RAGAnalysisResult.cs       # RAG analysis results (enhanced) ⭐
│   └── ItemValidationResult.cs    # Item validation model ⭐ NEW
├── Data/                 # Data layer
│   ├── ClaimDatabaseManager.cs    # Database operations (enhanced)
│   ├── multiple_users.json        # Sample fraud data (enhanced) ⭐
│   └── hsa_items.json             # HSA eligible items database ⭐ NEW
├── Frontend/frontend/    # React application
│   ├── src/                       # React source code (enhanced UI)
│   └── public/                    # Static assets
└── wwwroot/             # Built React app (production)
```

### Enhanced Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.SemanticKernel | 1.61.0 | AI orchestration |
| Microsoft.KernelMemory.Core | 0.95.x | RAG implementation |
| Microsoft.ML.LightGbm | 4.0.2 | Fraud detection ML |
| Azure.AI.FormRecognizer | 4.1.0 | Receipt OCR |
| Microsoft.Data.Sqlite | 9.0.7 | Database operations |

### Enhanced RAG Components ⭐ v2.0

1. **Enhanced RAGService** (`Services/RAGService.cs`)
   - Manages fraud knowledge base indexing and searching
   - Provides contextual analysis using historical patterns
   - **NEW**: Item validation pattern learning and analysis
   - Implements fallback local storage for reliability

2. **Enhanced FraudKnowledgeEntry** (`Models/FraudKnowledgeEntry.cs`)
   - Structured representation of fraud cases in knowledge base
   - Includes contextual metadata and risk factors
   - **NEW**: Item validation data and suspicious item patterns

3. **Enhanced RAGAnalyzeController** (`Controllers/RAGAnalyzeController.cs`)
   - Enhanced fraud analysis endpoints with item validation
   - Combines ML predictions with RAG insights and item analysis
   - **NEW**: HSA item catalog management and validation endpoints

### Adding New Fraud Patterns ⭐ Enhanced Process

1. **Define the pattern** in fraud detection logic (ML + Item validation)
2. **Create analysis methods** in RAGService with item validation rules
3. **Update knowledge indexing** to include new metadata and item patterns
4. **Test with sample data** to verify detection accuracy
5. **NEW**: Update HSA item validation rules as needed

## :rocket: Enhanced Deployment

### Development
- Backend: `dotnet run` (https://localhost:7041)
- Frontend: `npm start` (http://localhost:3000)
- **NEW**: Enhanced logging for item validation debugging

### Production
- Build React app: `npm run build` in Frontend/frontend
- Copy build output to wwwroot/
- Deploy .NET application to your preferred hosting service
- Configure environment variables for API keys
- **NEW**: Ensure HSA item database is properly deployed

## :lock: Security Considerations

- **API Keys**: Never commit API keys to source control
- **User Secrets**: Use `dotnet user-secrets` for development
- **Environment Variables**: Use for production deployment
- **CORS**: Configure appropriate origins for production
- **HTTPS**: Always use HTTPS in production
- **NEW**: HSA item data validation to prevent injection attacks

## :wrench: Enhanced Troubleshooting

### Common Issues

**"WEX_OPENAI_KEY environment variable is not set"**
- Configure your API key using one of the methods in the setup section

**"Failed to initialize RAG Knowledge Base"**
- Check that your WEX API key is valid
- Verify network connectivity to WEX AI Gateway
- Application will continue to work without RAG features

**"Item validation not working properly"** ⭐ NEW
- Verify HSA item database is properly loaded
- Check FormRecognizer service configuration
- Review item validation logs for debugging

**React app not loading**
- Ensure Node.js 18+ is installed
- Run `npm install` in Frontend/frontend directory
- Check CORS configuration if API calls fail

**Database errors**
- SQLite database is created automatically
- Ensure write permissions in project directory
- Delete ClaimsDB1.sqlite to reset database

**RAG analysis not working**
- Check API key configuration in appsettings.json
- Verify knowledge base initialization in startup logs
- Test endpoints using Swagger UI

### Getting Help

- Check application logs for detailed error messages
- Enable debug logging in appsettings.Development.json
- Review the enhanced sample data in Data/multiple_users.json
- Test API endpoints using Swagger UI at /swagger
- **NEW**: Review item validation logs for HSA eligibility debugging

## :handshake: Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Enhancement Guidelines ⭐ NEW
- Follow the RAG-first approach for new fraud detection features
- Include item validation considerations in new fraud patterns
- Maintain backward compatibility with existing endpoints
- Add comprehensive logging for debugging capabilities
- Update documentation for new features

## :page_facing_up: License

This project is proprietary software developed for WEX Inc.

## :office: About WEX

WEX Inc. is a leading financial technology service provider. This HSA Receipt Analyzer demonstrates WEX's commitment to innovation in healthcare financial services and fraud prevention.

---

**Built with :sparkles: by the WEX Technology Team**

*Your fraud detection system now has institutional memory, can reason about new cases in the context of historical patterns, and intelligently validates HSA item eligibility - providing comprehensive fraud protection with human-level reasoning capabilities.*