import React, { useState, useEffect } from "react";
import AIClaimAssistant from "./aiAssistant";
import "./StartPage.css";

// Speedometer-style Fraud Score Meter Component
const FraudScoreMeter = ({ score }) => {
  const [animatedScore, setAnimatedScore] = useState(0);
  
  useEffect(() => {
    const timer = setTimeout(() => {
      setAnimatedScore(score);
    }, 500);
    return () => clearTimeout(timer);
  }, [score]);

  const getColor = (score) => {
    if (score <= 30) return "#10b981"; // Green
    if (score <= 70) return "#f59e0b"; // Yellow
    return "#ef4444"; // Red
  };

  const getStatus = (score) => {
    if (score <= 30) return "Legitimate";
    if (score <= 70) return "Suspicious";
    return "Fraud";
  };

  // Convert score (0-100) to angle (-90 to 90 degrees)
  const angle = (animatedScore / 100) * 180 - 90;
  
  return (
    <div className="flex flex-col items-center">
      {/* Speedometer */}
      <div className="relative w-24 h-12 mb-2">
        <svg 
          width="96" 
          height="48" 
          viewBox="0 0 96 48" 
          className="transform"
        >
          {/* Background arc */}
          <path
            d="M 8 40 A 32 32 0 0 1 88 40"
            fill="none"
            stroke="#e5e7eb"
            strokeWidth="8"
            strokeLinecap="round"
          />
          
          {/* Green section (0-30%) */}
          <path
            d="M 8 40 A 32 32 0 0 1 34.4 16.8"
            fill="none"
            stroke="#10b981"
            strokeWidth="8"
            strokeLinecap="round"
            opacity="0.7"
          />
          
          {/* Yellow section (30-70%) */}
          <path
            d="M 34.4 16.8 A 32 32 0 0 1 61.6 16.8"
            fill="none"
            stroke="#f59e0b"
            strokeWidth="8"
            strokeLinecap="round"
            opacity="0.7"
          />
          
          {/* Red section (70-100%) */}
          <path
            d="M 61.6 16.8 A 32 32 0 0 1 88 40"
            fill="none"
            stroke="#ef4444"
            strokeWidth="8"
            strokeLinecap="round"
            opacity="0.7"
          />
          
          {/* Score indicators */}
          <text x="8" y="45" fill="#6b7280" fontSize="8" textAnchor="middle">0</text>
          <text x="48" y="12" fill="#6b7280" fontSize="8" textAnchor="middle">50</text>
          <text x="88" y="45" fill="#6b7280" fontSize="8" textAnchor="middle">100</text>
          
          {/* Center dot */}
          <circle cx="48" cy="40" r="3" fill="#374151" />
          
          {/* Animated arrow pointer */}
          <g transform={`rotate(${angle} 48 40)`}>
            <line
              x1="48"
              y1="40"
              x2="48"
              y2="20"
              stroke={getColor(animatedScore)}
              strokeWidth="2"
              strokeLinecap="round"
              className="transition-all duration-700 ease-out"
              style={{
                filter: `drop-shadow(0 2px 4px ${getColor(animatedScore)}40)`
              }}
            />
            {/* Arrow tip */}
            <polygon
              points="48,18 46,22 50,22"
              fill={getColor(animatedScore)}
              className="transition-all duration-700 ease-out"
            />
          </g>
        </svg>
      </div>
    </div>
  );
  };

// Loading Spinner Component
const LoadingSpinner = ({ isVisible, message = "Processing...", subMessage = "Please wait while we analyze your request" }) => {
  if (!isVisible) return null;

  return (
    <div className="loading-overlay">
      <div className="spinner-container">
        <div className="main-spinner"></div>
        <p className="spinner-text">{message}</p>
        <p className="spinner-subtext">{subMessage}</p>
      </div>
    </div>
  );
};

const initialClaims = [
  {
    userId: "USR001",
    date: "2025-07-01",
    amount: "$100.00",
    merchant: "Sunrise Dental",
    description: "Dental Cleaning",
    fraudScore: 15,
    status: "Legit"
  },
  {
    userId: "USR002",
    date: "2025-07-10",
    amount: "$250.00",
    merchant: "FitZone Gym",
    description: "Annual Gym",
    fraudScore: 72,
    status: "Fraud"
  },
  {
    userId: "USR001",
    date: "2025-07-15",
    amount: "$45.99",
    merchant: "HealthMart Pharmacy",
    description: "Prescription Medicine",
    fraudScore: 8,
    status: "Legit"
  },
  {
    userId: "USR003",
    date: "2025-07-18",
    amount: "$150.00",
    merchant: "QuickCare Clinic",
    description: "Urgent Care Visit",
    fraudScore: 55,
    status: "Suspicious"
  },
  {
    userId: "USR001",
    date: "2025-07-22",
    amount: "$89.99",
    merchant: "WellVision Eye Center",
    description: "Eye Exam",
    fraudScore: 25,
    status: "Legit"
  }
];

function StartPage() {
  const [activeTab, setActiveTab] = useState("claims");
  const [showForm, setShowForm] = useState(false);
  const [claims, setClaims] = useState(initialClaims);
  const [result, setResult] = useState("");
  const [resultVisible, setResultVisible] = useState(false);
  const [adminPrompt, setAdminPrompt] = useState("");
  const [adminResponse, setAdminResponse] = useState("");
  const [adminLoading, setAdminLoading] = useState(false);
  const [chatHistory, setChatHistory] = useState([]);
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [dragActive, setDragActive] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("date");
  const [filterBy, setFilterBy] = useState("all");
  
  // Loading spinner state
  const [isLoading, setIsLoading] = useState(false);
  const [loadingMessage, setLoadingMessage] = useState("Processing...");
  const [loadingSubMessage, setLoadingSubMessage] = useState("Please wait while we analyze your request");

  // Loading spinner control functions
  const showSpinner = (message = "Processing...", subMessage = "Please wait while we analyze your request") => {
    setLoadingMessage(message);
    setLoadingSubMessage(subMessage);
    setIsLoading(true);
  };

  const hideSpinner = () => {
    setIsLoading(false);
  };

  // Simulate processing delay for search/filter/sort operations
  const simulateProcessing = async (operation, delay = 800) => {
    switch(operation) {
      case 'search':
        showSpinner("Searching Claims...", "Filtering through transaction records");
        break;
      case 'filter':
        showSpinner("Applying Filters...", "Categorizing claims by risk level");
        break;
      case 'sort':
        showSpinner("Sorting Results...", "Organizing claims by selected criteria");
        break;
      default:
        showSpinner();
    }
    
    // Simulate processing time
    await new Promise(resolve => setTimeout(resolve, delay));
    hideSpinner();
  };

  // Predefined sequential questions for the chatbot
  const predefinedPrompts = {
    HighRiskVendors: [
      "Identify vendors involved in multiple fraudulent claims.",
      "List vendors with high fraud rates across users.",
    ],
    ClaimPatternClassifier: [
      "Classify claims based on repeated amounts or service types.",
      "Identify common patterns among suspicious claims.",
    ],
    SharedReceiptSummary: [
      "Find receipts submitted by multiple users.",
    ],
    SuspiciousUserNetwork: [
      "Detect users who share addresses or vendors.",
    ]
  };

  // Drag and drop handlers
  const handleDrag = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      const file = e.dataTransfer.files[0];
      const input = document.querySelector('input[name="image"]');
      if (input) {
        const dt = new DataTransfer();
        dt.items.add(file);
        input.files = dt.files;
      }
    }
  };

  // Enhanced search handler with loading
  const handleSearchChange = async (e) => {
    const value = e.target.value;
    setSearchTerm(value);
    
    if (value.length > 0) {
      await simulateProcessing('search', 600);
    }
  };

  // Enhanced filter handler with loading
  const handleFilterChange = async (e) => {
    const value = e.target.value;
    setFilterBy(value);
    await simulateProcessing('filter', 500);
  };

  // Enhanced sort handler with loading
  const handleSortChange = async (e) => {
    const value = e.target.value;
    setSortBy(value);
    await simulateProcessing('sort', 400);
  };

  // Filter and sort claims
  const filteredAndSortedClaims = claims
    .filter(claim => {
      if (filterBy === "all") return true;
      if (filterBy === "fraud" && claim.fraudScore > 70) return true;
      if (filterBy === "suspicious" && claim.fraudScore > 30 && claim.fraudScore <= 70) return true;
      if (filterBy === "legit" && claim.fraudScore <= 30) return true;
      return false;
    })
    .filter(claim => 
      searchTerm === "" || 
      claim.userId.toLowerCase().includes(searchTerm.toLowerCase()) ||
      claim.merchant.toLowerCase().includes(searchTerm.toLowerCase()) ||
      claim.description.toLowerCase().includes(searchTerm.toLowerCase())
    )
    .sort((a, b) => {
      if (sortBy === "date") return new Date(b.date) - new Date(a.date);
      if (sortBy === "amount") return parseFloat(b.amount.replace('$', '')) - parseFloat(a.amount.replace('$', ''));
      if (sortBy === "fraudScore") return b.fraudScore - a.fraudScore;
      return 0;
    });

  const handleAdminSubmit = async (e) => {
    e.preventDefault();
    if (!adminPrompt.trim()) return;

    setAdminLoading(true);
    showSpinner("Analyzing Claims Data...", "AI is processing your fraud detection request");
    
    try {
      const response = await fetch("/Analyze/adminAnalyze", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ Prompt: adminPrompt }),
      });
      const resultText = await response.text();
      setAdminResponse(resultText);
    } catch (err) {
      console.error("Admin analysis failed:", err);
      setAdminResponse("Error processing admin request. Please try again.");
    } finally {
      setAdminLoading(false);
      hideSpinner();
    }
  };

  const handlePredefinedQuestion = (question) => {
    handleAdminSubmit(question.question);
  };

  const clearChat = () => {
    setChatHistory([]);
    setAdminResponse("");
    setCurrentQuestionIndex(0);
  };

  const toggleForm = () => setShowForm((v) => !v);

  const handleSubmit = async (e) => {
    e.preventDefault();
    const form = e.target;
    
    // Validate that a file is selected
    const imageFile = form.image.files[0];
    if (!imageFile) {
      alert("Please select a receipt image before submitting.");
      return;
    }
    
    setIsSubmitting(true);
    showSpinner("Analyzing Receipt...", "AI is processing your claim for fraud detection");
    
    // Create FormData and explicitly add all form fields
    const formData = new FormData();
    
    // Add all text fields explicitly
    formData.append('userId', form.userId.value);
    formData.append('date', form.date.value);
    formData.append('amount', form.amount.value);
    formData.append('merchant', form.merchant.value);
    formData.append('description', form.description.value);
    formData.append('customPrompt', form.customPrompt.value || '');
    
    // Add the image file
    formData.append('image', imageFile);
    
    // Log form data for debugging
    console.log("Submitting form data:");
    console.log("userId:", form.userId.value);
    console.log("date:", form.date.value);
    console.log("amount:", form.amount.value);
    console.log("merchant:", form.merchant.value);
    console.log("description:", form.description.value);
    console.log("customPrompt:", form.customPrompt.value || '');
    console.log("image:", imageFile.name, `(${imageFile.size} bytes, ${imageFile.type})`);
    
    try {
      console.log("Making request to /Analyze/fraud-check...");
      console.log("FormData contents:");
      for (let [key, value] of formData.entries()) {
        if (value instanceof File) {
          console.log(`${key}:`, `File(${value.name}, ${value.size} bytes, ${value.type})`);
        } else {
          console.log(`${key}:`, value);
        }
      }
      
      const response = await fetch("api/RAGAnalyze/enhanced-fraud-check", {
        method: "POST",
        body: formData,
      });
      
      console.log("Response status:", response.status);
      console.log("Response headers:", Object.fromEntries(response.headers.entries()));
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error("Error response body:", errorText);
        throw new Error(`HTTP error! status: ${response.status} - ${response.statusText}. Response: ${errorText}`);
      }
      
      const resultData = await response.json();
      console.log("API Response:", resultData);
      
      // Extract userReadableText for display
      const displayText = resultData.userReadableText || JSON.stringify(resultData, null, 2);
      setResult(displayText);
      setResultVisible(true);
      
      // Extract fraud score from response
      const fraudScore = resultData.fraudScore || Math.floor(Math.random() * 100);
      const getStatus = (score) => {
        if (score <= 30) return "Legit";
        if (score <= 70) return "Suspicious";
        return "Fraud";
      };
      
      // Add the new claim to the table with fraud score
      setClaims([
        {
          userId: form.userId.value,
          date: form.date.value,
          amount: `$${parseFloat(form.amount.value).toFixed(2)}`,
          merchant: form.merchant.value,
          description: form.description.value,
          fraudScore: fraudScore,
          status: getStatus(fraudScore)
        },
        ...claims
      ]);
      form.reset();
      setShowForm(false);
      
      // Switch to admin tab to show the detailed result
      setActiveTab("admin");
    } catch (err) {
      console.error("Error analyzing claim:", err);
      setResult(`Error analyzing claim: ${err.message}`);
      setResultVisible(true);
    } finally {
      setIsSubmitting(false);
      hideSpinner();
    }
  };

  return (
    <div className="bg-gradient-to-br from-gray-50 to-blue-50 text-gray-900 min-h-screen">
      {/* Loading Spinner Overlay */}
      <LoadingSpinner 
        isVisible={isLoading} 
        message={loadingMessage} 
        subMessage={loadingSubMessage} 
      />

      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-blue-400 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-blob"></div>
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-purple-400 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-blob animation-delay-2000"></div>
        <div className="absolute top-40 left-40 w-80 h-80 bg-yellow-400 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-blob animation-delay-4000"></div>
      </div>

      <div className="relative z-10 flex flex-col items-center justify-start py-8">
        <div className="w-full max-w-7xl mx-auto px-8">
          {/* Enhanced Header with WEX Logo */}
          <div className="flex items-center justify-center mb-8 bg-white/80 backdrop-blur-sm rounded-2xl shadow-xl border border-white/20 p-8 transform hover:scale-105 transition-all duration-300">
            <div className="flex items-center space-x-6">
              {/* Official WEX Logo */}
              <div className="flex items-center justify-center bg-gradient-to-br from-white to-gray-50 rounded-xl p-4 shadow-lg">
                <img 
                  src="https://www.wexinc.com/wp-content/uploads/2023/04/Logo.svg" 
                  alt="WEX Logo" 
                  className="h-16 w-auto object-contain"
                />
              </div>
              
              {/* Title and Subtitle */}
              <div className="text-left">
                <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent mb-2">
                  HSA Receipt Fraud Analyzer
                </h1>
                <p className="text-lg text-gray-600 font-medium flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-2 animate-pulse"></span>
                  Powered by WEX AI Technology
                </p>
                <div className="flex items-center mt-2 text-sm text-gray-500">
                  <span className="mr-4">🔒 Secure</span>
                  <span className="mr-4">⚡ Real-time</span>
                  <span>🎯 Accurate</span>
                </div>
              </div>
            </div>
          </div>
      
      {/* Enhanced Tab Navigation */}
      <div className="mb-8 flex justify-center">
        <nav className="flex space-x-2 bg-white/60 backdrop-blur-lg p-2 rounded-2xl shadow-lg border border-white/20">
          <button
            onClick={() => setActiveTab("claims")}
            className={`px-6 py-3 rounded-xl font-semibold transition-all duration-300 flex items-center space-x-2 ${
              activeTab === "claims"
                ? "bg-gradient-to-r from-blue-500 to-blue-600 text-white shadow-lg transform scale-105"
                : "text-gray-600 hover:bg-white/50 hover:text-blue-600"
            }`}
          >
            <span>📋</span>
            <span>Claims Management</span>
            {activeTab === "claims" && (
              <div className="w-2 h-2 bg-white rounded-full animate-pulse"></div>
            )}
          </button>
          <button
            onClick={() => setActiveTab("admin")}
            className={`px-6 py-3 rounded-xl font-semibold transition-all duration-300 flex items-center space-x-2 ${
              activeTab === "admin"
                ? "bg-gradient-to-r from-purple-500 to-purple-600 text-white shadow-lg transform scale-105"
                : "text-gray-600 hover:bg-white/50 hover:text-purple-600"
            }`}
          >
            <span>👨‍💼</span>
            <span>Administrator</span>
            {activeTab === "admin" && (
              <div className="w-2 h-2 bg-white rounded-full animate-pulse"></div>
            )}
          </button>
        </nav>
      </div>

      {/* Enhanced Claims Tab */}
      {activeTab === "claims" && (
        <div className="w-full flex flex-col items-center animate-fadeIn">
          {/* Claims Table Section */}
          <div className="mb-8 w-full max-w-7xl">
            <div className="text-center mb-8">
              <h2 className="text-3xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent mb-3">
                📋 Claims Portfolio
              </h2>
              <p className="text-gray-600 text-lg">Monitor and manage HSA claims with real-time fraud detection</p>
            </div>

            {/* Search and Filter Controls */}
            <div className="mb-6 bg-gradient-to-r from-white/90 to-blue-50/90 backdrop-blur-sm rounded-2xl p-8 shadow-xl border border-white/30">
              {/* Search Input with Enhanced Styling */}
              <div className="mb-6">
                <label className="block text-sm font-semibold text-gray-700 mb-3">
                  Search Claims
                </label>
                <div className="relative max-w-lg mx-auto">
                  
                  <input
                    type="text"
                    placeholder="Search by User ID, Merchant, or Description..."
                    value={searchTerm}
                    onChange={handleSearchChange}
                    className="w-full pl-12 pr-4 py-4 border-2 border-gray-200 rounded-xl focus:ring-3 focus:ring-blue-300 focus:border-blue-500 transition-all bg-white/80 shadow-sm hover:shadow-md text-gray-700 placeholder-gray-400"
                  />
                </div>
              </div>

              {/* Filter and Sort Controls with Enhanced Styling */}
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Filter by Risk */}
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-3">
                    Filter by Risk Level
                  </label>
                  <div className="relative">
                    <select
                      value={filterBy}
                      onChange={handleFilterChange}
                      className="w-full px-4 py-4 border-2 border-gray-200 rounded-xl focus:ring-3 focus:ring-blue-300 focus:border-blue-500 transition-all bg-white/80 shadow-sm hover:shadow-md appearance-none text-gray-700 font-medium"
                    >
                      <option value="all">All Risk Levels</option>
                      <option value="fraud">High Risk (Fraud)</option>
                      <option value="suspicious">Medium Risk (Suspicious)</option>
                      <option value="legit">Low Risk (Legitimate)</option>
                    </select>
                  </div>
                </div>

                {/* Sort by */}
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-3">
                    Sort Claims By
                  </label>
                  <div className="relative">
                    <select
                      value={sortBy}
                      onChange={handleSortChange}
                      className="w-full px-4 py-4 border-2 border-gray-200 rounded-xl focus:ring-3 focus:ring-blue-300 focus:border-blue-500 transition-all bg-white/80 shadow-sm hover:shadow-md appearance-none text-gray-700 font-medium"
                    >
                      <option value="date">Date (Newest First)</option>
                      <option value="amount">Amount (Highest First)</option>
                      <option value="fraudScore">Risk Score (Highest First)</option>
                    </select>
                   
                  </div>
                </div>
              </div>

              {/* Results count with enhanced styling */}
              <div className="mt-6 text-center">
                <div className="inline-flex items-center bg-gradient-to-r from-blue-500 to-purple-600 text-white px-6 py-3 rounded-full shadow-lg">
                  <span className="text-sm font-semibold">
                    Showing {filteredAndSortedClaims.length} of {claims.length} claims
                  </span>
                  {filteredAndSortedClaims.length !== claims.length && (
                    <span className="ml-2 bg-white/20 px-2 py-1 rounded-full text-xs">
                      Filtered
                    </span>
                  )}
                </div>
              </div>
            </div>

            {/* Enhanced Claims Table */}
            <div className="overflow-hidden bg-white/80 backdrop-blur-sm rounded-2xl shadow-xl border border-white/20">
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 text-white">
                    <tr>
                      <th className="py-5 px-6 text-center font-bold text-sm uppercase tracking-wider">User ID</th>
                      <th className="py-5 px-6 text-center font-bold text-sm uppercase tracking-wider">Date</th>
                      <th className="py-5 px-6 text-center font-bold text-sm uppercase tracking-wider">Amount</th>
                      <th className="py-5 px-6 text-center font-bold text-sm uppercase tracking-wider">Merchant</th>
                      <th className="py-5 px-6 text-center font-bold text-sm uppercase tracking-wider">Description</th>
                      <th className="py-5 px-6 text-center font-bold text-sm uppercase tracking-wider">Status</th>
                      <th className="py-5 px-6 text-center font-bold text-sm uppercase tracking-wider">Risk Assessment</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {filteredAndSortedClaims.map((claim, idx) => (
                      <tr
                        key={idx}
                        className={`hover:bg-blue-50/50 transition-all duration-200 hover:shadow-md ${
                          idx % 2 === 1 ? "bg-gray-50/30" : "bg-white/50"
                        }`}
                      >
                        <td className="py-5 px-6 text-center">
                          <span className="bg-gradient-to-r from-blue-100 to-purple-100 text-blue-800 px-4 py-2 rounded-full text-sm font-mono font-bold shadow-sm">
                            {claim.userId}
                          </span>
                        </td>
                        <td className="py-5 px-6 text-center text-gray-700 font-medium">
                          {new Date(claim.date).toLocaleDateString()}
                        </td>
                        <td className="py-5 px-6 text-center font-bold text-lg text-green-600">
                          {claim.amount}
                        </td>
                        <td className="py-5 px-6 text-center text-gray-800 font-medium">
                          {claim.merchant}
                        </td>
                        <td className="py-5 px-6 text-center text-gray-600">
                          {claim.description}
                        </td>
                        <td className="py-5 px-6 text-center">
                          <span className={`px-3 py-1 rounded-full text-sm font-bold ${
                            claim.fraudScore >= 70 
                              ? 'bg-red-100 text-red-800' 
                              : claim.fraudScore >= 40 
                              ? 'bg-yellow-100 text-yellow-800' 
                              : 'bg-green-100 text-green-800'
                          }`}>
                            {claim.fraudScore >= 70 ? 'Fraud' : claim.fraudScore >= 40 ? 'Suspicious' : 'Legitimate'} ({claim.fraudScore}%)
                          </span>
                        </td>
                        <td className="py-5 px-6 text-center">
                          <FraudScoreMeter score={claim.fraudScore || 0} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
          
          {/* Enhanced Add Claim Button */}
          <div className="flex justify-center mb-8">
            <button
              onClick={toggleForm}
              className="group bg-gradient-to-r from-emerald-500 via-green-500 to-teal-500 text-white px-10 py-4 rounded-2xl hover:from-emerald-600 hover:via-green-600 hover:to-teal-600 transition-all duration-300 shadow-xl hover:shadow-2xl font-bold text-lg flex items-center space-x-3 transform hover:scale-105"
            >
              <span className="text-2xl group-hover:animate-bounce">➕</span>
              <span>Submit New Claim</span>
              <div className="w-2 h-2 bg-white rounded-full opacity-60 group-hover:animate-ping"></div>
            </button>
          </div>
          
          {/* Claim Form */}
          {showForm && (
            <div className="flex justify-center w-full mb-8">
              <form
                id="claimForm"
                className="bg-white p-8 shadow-xl rounded-xl border border-gray-200 w-full max-w-3xl"
                onSubmit={handleSubmit}
              >
                <div className="text-center mb-6">
                  <h2 className="text-2xl font-bold text-gray-800 mb-2">📝 New Claim Submission</h2>
                  <p className="text-gray-600">Fill out the details below for fraud analysis</p>
                </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">👤 User ID</label>
                  <input
                    name="userId"
                    type="text"
                    required
                    className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                    placeholder="Enter User ID"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">📅 Service Date</label>
                  <input
                    name="date"
                    type="date"
                    required
                    className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">💰 Amount</label>
                  <input
                    name="amount"
                    type="number"
                    step="0.01"
                    required
                    className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                    placeholder="0.00"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">🏪 Merchant</label>
                  <input
                    name="merchant"
                    type="text"
                    required
                    className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                    placeholder="Merchant Name"
                  />
                </div>
                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-2">📝 Description</label>
                  <input
                    name="description"
                    type="text"
                    required
                    className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                    placeholder="Service or item description"
                  />
                </div>
              </div>
              
              {/* Receipt Upload */}
              <div className="mb-6">
                <label className="block text-sm font-medium text-gray-700 mb-2">🧾 Upload Receipt</label>
                <div className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center hover:border-blue-400 transition-colors">
                  <input
                    name="image"
                    type="file"
                    accept=".pdf,.jpg,.png"
                    className="w-full"
                  />
                  <p className="text-sm text-gray-500 mt-2">PDF, JPG, or PNG files accepted</p>
                </div>
              </div>
              
              {/* Optional Prompt */}
              <div className="mb-8">
                <label className="block text-sm font-medium text-gray-700 mb-2">🧠 Additional Notes (Optional)</label>
                <textarea
                  name="customPrompt"
                  rows="3"
                  className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                  placeholder="Any additional context or special instructions..."
                ></textarea>
              </div>
              
              <div className="flex justify-center space-x-4">
                <button
                  type="button"
                  onClick={() => setShowForm(false)}
                  className="px-6 py-3 bg-gray-500 text-white rounded-lg hover:bg-gray-600 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-8 py-3 bg-gradient-to-r from-blue-500 to-blue-600 text-white rounded-lg hover:from-blue-600 hover:to-blue-700 transition-all duration-200 shadow-lg hover:shadow-xl font-semibold"
                  disabled={isSubmitting}
                >
                  🔍 Analyze & Submit
                </button>
              </div>
              </form>
            </div>
          )}
        </div>
      )}

      {/* Administrator Tab */}
      {activeTab === "admin" && (
        <div className="w-full flex flex-col items-center">
          <div className="w-full max-w-6xl bg-white rounded-xl shadow-xl p-8">
            <div className="text-center mb-8">
              <h2 className="text-3xl font-bold text-gray-800 mb-2">👨‍💼 Administrator Dashboard</h2>
              <p className="text-gray-600 text-lg">
                Advanced fraud analysis and AI-powered claims investigation
              </p>
            </div>
            
            {/* Show Analysis Result if available */}
            {resultVisible && result && (
              <div className="mb-6 p-8 bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded-xl shadow-lg">
                <div className="flex items-center justify-between mb-6">
                  <h3 className="text-2xl font-bold text-blue-900 flex items-center">
                    🔍 Fraud Analysis Report
                  </h3>
                  <button 
                    onClick={() => setResultVisible(false)}
                    className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
                  >
                    ✕ Close
                  </button>
                </div>
                
                <div className="bg-white rounded-lg p-6 shadow-inner border border-gray-100">
                  <div 
                    className="prose prose-lg max-w-none text-gray-800"
                    style={{
                      lineHeight: '1.7',
                      fontFamily: '"Inter", "Segoe UI", system-ui, sans-serif'
                    }}
                    dangerouslySetInnerHTML={{
                      __html: result
                        .replace(/## (.*)/g, '<h2 class="text-xl font-bold text-blue-800 mt-6 mb-3 pb-2 border-b border-blue-200">$1</h2>')
                        .replace(/### (.*)/g, '<h3 class="text-lg font-semibold text-blue-700 mt-4 mb-2">$1</h3>')
                        .replace(/\*\*(.*?)\*\*/g, '<strong class="font-bold text-gray-900">$1</strong>')
                        .replace(/• (.*)/g, '<li class="ml-4 mb-1 text-gray-700">$1</li>')
                        .replace(/⚠ (.*)/g, '<div class="bg-yellow-100 border-l-4 border-yellow-500 p-3 my-3"><span class="text-yellow-800 font-medium">⚠️ $1</span></div>')
                        .replace(/Confidence Level: (.*)/g, '<div class="mt-4 p-3 bg-blue-100 border border-blue-300 rounded-lg"><strong class="text-blue-800">🎯 Confidence Level:</strong> <span class="font-semibold text-blue-900">$1</span></div>')
                        .replace(/\n/g, '<br>')
                    }}
                  />
                </div>
                
                <div className="mt-6 flex justify-center">
                  <div className="bg-white rounded-lg p-4 shadow-md border border-gray-200">
                    <p className="text-sm text-gray-600 text-center">
                      📊 Analysis powered by WEX AI Fraud Detection System
                    </p>
                  </div>
                </div>
              </div>
            )}
            
            {/* AI Claims Assistant Section */}
            {!resultVisible && (
              <div className="bg-gradient-to-r from-gray-50 to-blue-50 rounded-xl p-6 border border-gray-200">
                <div className="text-center mb-6">
                  <h3 className="text-xl font-semibold text-gray-800 mb-2">🤖 FraudLens AI Assistant</h3>
                  <p className="text-gray-600">
                    Ask questions about fraud patterns, user behaviors, and claim analysis
                  </p>
                </div>
                
                <div className="flex justify-center">
                  <div className="w-full max-w-lg">
                    <AIClaimAssistant />
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
        </div>
      </div>
    </div>
  );
}

export default StartPage;
