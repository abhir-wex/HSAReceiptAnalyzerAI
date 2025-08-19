# ?? HSA Receipt Analyzer with RAG Implementation

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![React](https://img.shields.io/badge/React-18.x-blue.svg)](https://reactjs.org/)
[![Azure](https://img.shields.io/badge/Azure-AI%20Services-0078d4.svg)](https://azure.microsoft.com/en-us/services/cognitive-services/)
[![WEX](https://img.shields.io/badge/Powered%20by-WEX%20Technology-orange.svg)](https://www.wexinc.com/)

A sophisticated **Healthcare Savings Account (HSA) receipt fraud detection system** that combines traditional Machine Learning with **Retrieval-Augmented Generation (RAG)** to provide intelligent, context-aware fraud analysis.

## ?? Overview

This system revolutionizes fraud detection by combining:
- **Traditional ML Models** (LightGBM) for pattern-based fraud detection
- **RAG Technology** for contextual analysis using historical fraud patterns
- **Azure AI Services** for receipt OCR and data extraction
- **Semantic Kernel** for intelligent fraud reasoning
- **Real-time Learning** that improves with each fraud case

## ? Key Features

### ?? **Enhanced Fraud Detection**
- **Contextual Analysis**: "This $250 claim matches 3 historical fraud cases at HealthMart"
- **Evidence-Based Decisions**: Specific historical patterns referenced in analysis
- **Risk Factor Identification**: Round amounts, same-day submissions, IP anomalies
- **Intelligent Recommendations**: "Cross-reference receipt hash across all users"

### ?? **RAG-Powered Intelligence**
- **Knowledge Base**: Automatically indexed fraud cases with contextual metadata
- **Semantic Search**: Find similar fraud patterns across historical data
- **Continuous Learning**: System gets smarter with each new fraud case
- **Trend Analysis**: Identify evolving fraud schemes and emerging patterns

### ?? **Comprehensive Analytics**
- **Admin Dashboard**: Natural language queries about fraud patterns
- **Fraud Trends**: "Show me round amount fraud patterns in the last 6 months"
- **Merchant Risk Analysis**: Historical fraud rates by merchant
- **Pattern Evolution**: Track how fraud schemes change over time

## ?? Real-World RAG Use Cases

### 1. **Contextual Fraud Analysis**
- **What it does**: When analyzing a new claim, RAG searches through historical fraud cases to find similar patterns
- **Real example**: New claim for $250 at "HealthMart Pharmacy" ? RAG finds 3 similar fraud cases at same merchant with round amounts
- **Business value**: Provides investigators with specific historical context instead of generic ML scores

### 2. **Expert Knowledge Retention**
- **What it does**: Captures and retains fraud investigation expertise in a searchable knowledge base
- **Real example**: Experienced fraud analyst retires ? Their knowledge of specific fraud schemes remains accessible through RAG
- **Business value**: Institutional knowledge preservation and consistent fraud detection quality

### 3. **Pattern Evolution Detection**
- **What it does**: Identifies how fraud patterns evolve over time by comparing new cases to historical trends
- **Real example**: "SharedReceiptAcrossUsers" pattern evolving to include IP address manipulation
- **Business value**: Proactive fraud prevention and early detection of emerging schemes

## ??? Architecture

```
+---------------------+    +---------------------+    +---------------------+
|   React Frontend    |--->|  .NET 8 Web API     |--->|   Azure Services    |
|                     |    |                     |    |                     |
| - Receipt Upload    |    | - RAG Analysis      |    | - Form Recognizer   |
| - Admin Dashboard   |    | - ML Fraud Model    |    | - WEX AI Gateway    |
| - Results Display   |    | - Knowledge Base    |    | - OpenAI Models     |
+---------------------+    +---------------------+    +---------------------+
                                      |
                                      v
                           +---------------------+
                           |   SQLite Database   |
                           |                     |
                           | - Claims Data       |
                           | - Fraud Patterns    |
                           | - User History      |
                           +---------------------+
```

### RAG Technical Architecture
```
+---------------------+
|    New Claim        |
|    Analysis         |
+----------+----------+
           |
           v
+---------------------+    +---------------------+
|   Traditional ML    |    |    RAG Service      |
|   Fraud Detection   |<-->|                     |
|   (LightGBM)        |    |  - Knowledge Base   |
+---------------------+    |  - Semantic Search  |
           |                |  - Context Analysis |
           |                +---------------------+
           v                           |
+---------------------+               |
|   Combined Score    |<--------------+
|   & Analysis        |
+---------------------+
           |
           v
+---------------------+
|   Enhanced Response |
|   - ML Score        |
|   - RAG Analysis    |
|   - Historical Cases|
|   - Risk Factors    |
|   - Recommendations |
+---------------------+
```

## ?? Getting Started

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

## ?? API Endpoints

### ?? RAG-Enhanced Endpoints

#### `POST /api/RAGAnalyze/enhanced-fraud-check`
**RAG-enhanced fraud analysis with historical context**

```bash
curl -X POST "https://localhost:7041/api/RAGAnalyze/enhanced-fraud-check" \
  -H "Content-Type: multipart/form-data" \
  -F "image=@receipt.jpg"
```

**Enhanced Response with RAG Context:**
```json
{
  "claimId": "CLAIM-2025-001",
  "isFraudulent": true,
  "fraudScore": 87.5,
  "riskLevel": "High",
  "mlScore": 82.0,
  "ragConfidence": 0.89,
  "message": "?? FRAUD DETECTED (87.5%, High risk). This pattern matches 3 historical fraud case(s).",
  "ragAnalysis": "Based on historical patterns, this $250 claim at HealthMart matches SharedReceiptAcrossUsers template...",
  "similarHistoricalCases": [
    {
      "relevance": 0.92,
      "summary": "FRAUD CASE: $250 at HealthMart Pharmacy, SharedReceiptAcrossUsers pattern...",
      "source": "FraudKnowledgeBase"
    }
  ],
  "riskFactors": ["Round amount pattern", "Same-day submission"],
  "recommendedAction": "Cross-reference receipt hash across all users"
}
```

#### `POST /api/RAGAnalyze/contextual-admin-analysis`
**Natural language fraud analysis queries**

```bash
curl -X POST "https://localhost:7041/api/RAGAnalyze/contextual-admin-analysis" \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me fraud patterns at HealthMart Pharmacy with round amounts"}'
```

**Sample Admin Query Response:**
```json
{
  "query": "Show me fraud patterns at HealthMart Pharmacy with round amounts",
  "contextualAnalysis": "## Fraud Analysis: HealthMart Pharmacy Round Amount Patterns\n\n### Historical Evidence:\n- 5 confirmed fraud cases with round amounts ($100, $250, $500)\n- 'SharedReceiptAcrossUsers' template appears in 80% of cases\n- Geographic clustering: 3 cases from Phoenix area\n\n### Risk Assessment:\n- **HIGH RISK**: Round amounts correlate with 89% fraud rate\n- **Emerging Pattern**: Receipt hash duplication across users\n\n### Recommendations:\n1. Flag all HealthMart claims with round amounts\n2. Implement real-time receipt hash verification",
  "totalClaimsAnalyzed": 1250,
  "fraudCasesInKnowledgeBase": 85
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
**Traditional ML-based fraud detection**

#### `POST /Analyze/adminAnalyze`
**Pattern-based analysis with RAG fallback**

### Knowledge Base Management

#### `POST /api/RAGAnalyze/rebuild-knowledge-base`
**Rebuild the RAG knowledge base**

## ?? RAG vs Traditional Analysis Comparison

### Before RAG (Traditional):
```json
{
  "IsFraudulent": true,
  "FraudScore": 85,
  "Message": "?? High risk claim detected"
}
```

### After RAG (Enhanced):
```json
{
  "IsFraudulent": true,
  "FraudScore": 87.5,
  "RiskLevel": "High",
  "Message": "?? FRAUD DETECTED (87.5%, High risk). This pattern matches 3 historical fraud case(s). Key factors: Round amount pattern, Same-day submission.",
  "RAGAnalysis": "Based on historical fraud patterns, this claim shows HIGH similarity to confirmed fraud cases. The $250.00 amount at HealthMart Pharmacy matches the 'RoundAmountPattern' template seen in 3 previous cases.",
  "SimilarHistoricalCases": [...],
  "RiskFactors": ["Round amount pattern", "Same-day submission", "High similarity to known fraud cases"],
  "RecommendedAction": "Verify receipt authenticity and item details; Cross-reference receipt hash across all users"
}
```

## ?? Real Use Case Example

### Scenario: Investigating Suspicious $250 Claim

1. **New Claim Submitted**: 
   - Amount: $250.00
   - Merchant: HealthMart Pharmacy
   - User: USR0501

2. **RAG Analysis Process**:
   ```
   Search Query: "HealthMart Pharmacy $250 Medical Equipment"
   ?
   Knowledge Base Search Results:
   - 92% relevance: Previous fraud at HealthMart for $250 (SharedReceiptAcrossUsers)
   - 88% relevance: Round amount fraud pattern at HealthMart
   - 85% relevance: Same merchant, different user, similar timeframe
   ```

3. **Enhanced Investigation Result**:
   ```
   FRAUD ALERT: This claim matches known fraud pattern "SharedReceiptAcrossUsers"
   Historical Evidence: Receipt hash found in 2 previous fraud cases
   Investigation Priority: HIGH - Cross-reference immediately
   Recommended Action: Contact HealthMart to verify transaction authenticity
   ```

## ?? Configuration

### Key Configuration Sections

| Setting | Description | Required |
|---------|-------------|----------|
| `WEXOpenAI:Endpoint` | WEX AI Gateway endpoint | ? |
| `WEXOpenAI:Key` | WEX API key | ? |
| `FormRecognizer:Endpoint` | Azure Form Recognizer endpoint | ?? Optional |
| `FormRecognizer:Key` | Azure Form Recognizer key | ?? Optional |

### Database Configuration

The system uses SQLite by default:
- **Database**: `ClaimsDB1.sqlite` (auto-created)
- **Sample Data**: `Data/multiple_users.json`
- **Location**: Project root directory

### CORS Configuration

The API is configured to accept requests from:
- `http://localhost:3000` (React development server)
- Modify `Program.cs` to add additional origins for production

### RAG System Configuration

The RAG system automatically initializes on application startup:

```csharp
// In Program.cs
using (var scope = app.Services.CreateScope())
{
    var ragService = scope.ServiceProvider.GetRequiredService<IRAGService>();
    await ragService.InitializeKnowledgeBaseAsync(); // Indexes existing fraud cases
}
```

## ?? Testing the System

### 1. Test Enhanced Fraud Detection

1. Start both backend and frontend
2. Navigate to `http://localhost:3000`
3. Go to the "Claims" tab
4. Upload a receipt image or fill out the form manually
5. Observe the enhanced fraud analysis with RAG context

### 2. Test Admin Analysis

1. Go to the "Administrator" tab
2. Try queries like:
   - "Show me fraud patterns at HealthMart Pharmacy"
   - "Analyze round amount fraud trends"
   - "Find claims with same-day submissions"

### 3. Test RAG Knowledge Base

Use API tools like Postman or curl to test the RAG endpoints directly.

## ?? Sample Data

The system includes comprehensive sample data:
- **500+ claims** across multiple users
- **Known fraud patterns**: SharedReceiptAcrossUsers, RoundAmountPattern
- **Multiple merchants**: HealthMart Pharmacy, PharmaPoint, MediShop
- **Geographic distribution**: Phoenix, Chicago, Los Angeles, Houston, New York

### RAG Knowledge Base Content

The RAG system automatically indexes:
- ? **Confirmed Fraud Cases** from your `multiple_users.json`
- ? **Fraud Templates**: SharedReceiptAcrossUsers, RoundAmountPattern, etc.
- ? **Merchant Risk Patterns**: HealthMart, PharmaPoint, MediShop cases
- ? **Geographic Anomalies**: Location-based fraud indicators
- ? **Temporal Patterns**: Time-based fraud behaviors
- ? **IP Address Clustering**: Network-based fraud detection

## ?? Fraud Detection Capabilities

### Traditional ML Detection
- **Round amount patterns** ($100, $250, $500)
- **High frequency submissions** (multiple claims same day)
- **Unusual timing patterns** (late night, weekend submissions)
- **Geographic anomalies** (IP address clustering)

### RAG-Enhanced Detection
- **Historical pattern matching** with relevance scoring
- **Contextual risk assessment** based on similar cases
- **Evidence-based recommendations** with specific actions
- **Merchant risk profiling** using historical fraud rates

## ?? Business Impact

### Immediate Benefits:
- **Enhanced Accuracy**: Combined ML + RAG provides more accurate fraud detection
- **Contextual Insights**: Investigators see specific historical patterns instead of generic scores
- **Faster Investigation**: Relevant historical cases surface automatically
- **Knowledge Retention**: Fraud expertise captured and searchable

### Long-term Value:
- **Adaptive Learning**: System improves with each new fraud case
- **Pattern Evolution**: Detects emerging fraud schemes early
- **Institutional Knowledge**: Preserves investigator expertise
- **Proactive Prevention**: Trend analysis enables preventive measures

## ??? Development

### Project Structure

```
HSAReceiptAnalyzerAI/
??? Controllers/           # API controllers
?   ??? AnalyzeController.cs       # Traditional fraud detection
?   ??? RAGAnalyzeController.cs    # RAG-enhanced endpoints
?   ??? ClaimDatabaseController.cs # Database management
??? Services/             # Business logic services
?   ??? RAGService.cs              # RAG implementation
?   ??? FraudDetectionService.cs   # ML fraud detection
?   ??? SemanticKernelService.cs   # AI prompt handling
?   ??? FormRecognizerService.cs   # Receipt OCR
??? Models/               # Data models
?   ??? Claim.cs                   # Core claim model
?   ??? FraudKnowledgeEntry.cs     # RAG knowledge model
?   ??? RAGAnalysisResult.cs       # RAG analysis results
??? Data/                 # Data layer
?   ??? ClaimDatabaseManager.cs    # Database operations
?   ??? multiple_users.json       # Sample fraud data
??? Frontend/frontend/    # React application
?   ??? src/                       # React source code
?   ??? public/                    # Static assets
??? wwwroot/             # Built React app (production)
```

### Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.SemanticKernel | 1.61.0 | AI orchestration |
| Microsoft.KernelMemory.Core | 0.95.x | RAG implementation |
| Microsoft.ML.LightGbm | 4.0.2 | Fraud detection ML |
| Azure.AI.FormRecognizer | 4.1.0 | Receipt OCR |
| Microsoft.Data.Sqlite | 9.0.7 | Database operations |

### Key RAG Components

1. **RAGService** (`Services/RAGService.cs`)
   - Manages fraud knowledge base indexing and searching
   - Provides contextual analysis using historical patterns
   - Implements fallback local storage for reliability

2. **FraudKnowledgeEntry** (`Models/FraudKnowledgeEntry.cs`)
   - Structured representation of fraud cases in knowledge base
   - Includes contextual metadata and risk factors

3. **RAGAnalyzeController** (`Controllers/RAGAnalyzeController.cs`)
   - Enhanced fraud analysis endpoints
   - Combines ML predictions with RAG insights

### Adding New Fraud Patterns

1. **Define the pattern** in fraud detection logic
2. **Create analysis methods** in RAGService
3. **Update knowledge indexing** to include new metadata
4. **Test with sample data** to verify detection

## ?? Deployment

### Development
- Backend: `dotnet run` (https://localhost:7041)
- Frontend: `npm start` (http://localhost:3000)

### Production
- Build React app: `npm run build` in Frontend/frontend
- Copy build output to wwwroot/
- Deploy .NET application to your preferred hosting service
- Configure environment variables for API keys

## ?? Security Considerations

- **API Keys**: Never commit API keys to source control
- **User Secrets**: Use `dotnet user-secrets` for development
- **Environment Variables**: Use for production deployment
- **CORS**: Configure appropriate origins for production
- **HTTPS**: Always use HTTPS in production

## ?? Troubleshooting

### Common Issues

**"WEX_OPENAI_KEY environment variable is not set"**
- Configure your API key using one of the methods in the setup section

**"Failed to initialize RAG Knowledge Base"**
- Check that your WEX API key is valid
- Verify network connectivity to WEX AI Gateway
- Application will continue to work without RAG features

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
- Review the sample data in Data/multiple_users.json
- Test API endpoints using Swagger UI at /swagger

## ?? Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## ?? License

This project is proprietary software developed for WEX Inc.

## ?? About WEX

WEX Inc. is a leading financial technology service provider. This HSA Receipt Analyzer demonstrates WEX's commitment to innovation in healthcare financial services and fraud prevention.

---

**Built with ?? by the WEX Technology Team**

*Your fraud detection system now has institutional memory and can reason about new cases in the context of historical patterns.*