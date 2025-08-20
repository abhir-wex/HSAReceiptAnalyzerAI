import React, { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Button, Select } from "./components/UIComponents";
import ClaimCard from "./components/ClaimCard";
import ModernLoadingOverlay from "./components/ModernLoadingOverlay";
import ModernChatAssistant from "./components/ModernChatAssistant";
import ModernClaimForm from "./components/ModernClaimForm";
import "./StartPage.css";

// Enhanced Background Component with WEX colors
const AnimatedBackground = () => (
  <div className="fixed inset-0 overflow-hidden pointer-events-none">
    {/* WEX Light gradient background */}
    <div className="absolute inset-0 bg-wex-light" />
    
    {/* Floating Orbs with WEX colors */}
    { [
      { color: '#4A90E2', size: 200, x: '10%', y: '20%' }, // WEX Blue
      { color: '#17A2B8', size: 150, x: '80%', y: '10%' }, // WEX Teal
      { color: '#B3D9FF', size: 180, x: '20%', y: '70%' }, // WEX Light Blue
      { color: '#FFB81C', size: 120, x: '75%', y: '60%' }, // WEX Yellow
      { color: '#C8102E', size: 160, x: '60%', y: '80%' }, // WEX Red
      { color: '#FFFFFF', size: 140, x: '40%', y: '30%' }, // WEX White
    ].map((orb, i) => (
      <motion.div
        key={i}
        className="absolute rounded-full mix-blend-multiply filter blur-xl opacity-20"
        style={{
          width: orb.size,
          height: orb.size,
          background: `radial-gradient(circle, ${orb.color}, transparent)`,
          left: orb.x,
          top: orb.y,
        }}
        animate={{
          x: [0, Math.random() * 100 - 50],
          y: [0, Math.random() * 100 - 50],
          scale: [1, 1.2, 1],
          rotate: [0, 360],
        }}
        transition={{
          duration: Math.random() * 20 + 15,
          repeat: Infinity,
          repeatType: "reverse",
        }}
      />
    ))}
    
    {/* WEX Grid Pattern */}
    <div className="absolute inset-0 bg-grid-pattern opacity-10" />
    
    {/* Subtle gradient overlay */}
    <div className="absolute inset-0 bg-gradient-to-br from-white/30 via-transparent to-wex-blue/10" />
  </div>
);

// Initial claims data
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
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("date");
  const [filterBy, setFilterBy] = useState("all");
  
  // Loading spinner state
  const [isLoading, setIsLoading] = useState(false);
  const [loadingMessage, setLoadingMessage] = useState("Processing...");
  const [loadingSubMessage, setLoadingSubMessage] = useState("Please wait while we analyze your request");
  const [loadingProgress, setLoadingProgress] = useState(null);

  // Loading spinner control functions
  const showSpinner = (message = "Processing...", subMessage = "Please wait while we analyze your request", progress = null) => {
    setLoadingMessage(message);
    setLoadingSubMessage(subMessage);
    setLoadingProgress(progress);
    setIsLoading(true);
  };

  const hideSpinner = () => {
    setIsLoading(false);
    setLoadingProgress(null);
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
      if (sortBy === "amount" ) return parseFloat(b.amount.replace('$', '')) - parseFloat(a.amount.replace('$', ''));
      if (sortBy === "fraudScore") return b.fraudScore - a.fraudScore;
      return 0;
    });

  const toggleForm = () => setShowForm((v) => !v);

  const handleClaimSubmit = async (formData) => {
    setIsSubmitting(true);
    showSpinner(
      "Analyzing Receipt...", 
      "AI is processing your claim for fraud detection",
      0
    );
    
    // Simulate progress updates
    const progressSteps = [0, 25, 50, 75, 90, 100];
    for (let i = 0; i < progressSteps.length; i++) {
      await new Promise(resolve => setTimeout(resolve, 500));
      setLoadingProgress(progressSteps[i]);
    }
    
    try {
      console.log("Submitting form data...");
      
      const response = await fetch("api/RAGAnalyze/enhanced-fraud-check", {
        method: "POST",
        body: formData,
      });
      
      console.log("Response status:", response.status);
      
      if (!response.ok) {
        const errorText = await response.text();
        console.error("Error response body:", errorText);
        throw new Error(`HTTP error! status: ${response.status} - ${response.statusText}. Response: ${errorText}`);
      }
      
      const resultData = await response.json();
      console.log("API Response:", resultData);
      
      // Extract userReadableText for display
      const displayText = resultData.userReadableText || JSON.stringify(resultData, null, 2);
      
      // Extract fraud score from response
      const fraudScore = resultData.fraudScore || Math.floor(Math.random() * 100);
      const getStatus = (score) => {
        if (score <= 30) return "Legit";
        if (score <= 70) return "Suspicious";
        return "Fraud";
      };
      
      // Get form data values
      const userId = formData.get('userId');
      const date = formData.get('date');
      const amount = formData.get('amount');
      const merchant = formData.get('merchant');
      const description = formData.get('description');
      
      // Add the new claim to the table with fraud score
      setClaims([
        {
          userId: userId,
          date: date,
          amount: `$${parseFloat(amount).toFixed(2)}`,
          merchant: merchant,
          description: description,
          fraudScore: fraudScore,
          status: getStatus(fraudScore)
        },
        ...claims
      ]);
      
      // Close the form first
      setShowForm(false);
      
      // Set the result and make it visible
      setResult(displayText);
      setResultVisible(true);
      
      // Small delay to ensure state updates, then switch to admin tab
      setTimeout(() => {
        setActiveTab("admin");
      }, 100);
      
    } catch (err) {
      console.error("Error analyzing claim:", err);
      
      // Close the form
      setShowForm(false);
      
      // Set error result and make it visible
      setResult(`Error analyzing claim: ${err.message}`);
      setResultVisible(true);
      
      // Small delay to ensure state updates, then switch to admin tab
      setTimeout(() => {
        setActiveTab("admin");
      }, 100);
    } finally {
      setIsSubmitting(false);
      hideSpinner();
    }
  };

  return (
    <div className="min-h-screen relative">
      {/* WEX Animated Background */}
      <AnimatedBackground />
      
      {/* Loading Overlay */}
      <ModernLoadingOverlay 
        isVisible={isLoading} 
        message={loadingMessage} 
        subMessage={loadingSubMessage}
        progress={loadingProgress}
      />

      <div className="relative z-10 flex flex-col items-center justify-start py-8">
        <div className="w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          {/* Enhanced Header with WEX Logo and branding */}
          <motion.div
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
            className="flex items-center justify-center mb-8"
          >
            <div className="wex-header p-8 rounded-3xl shadow-2xl">
              <div className="flex flex-col lg:flex-row items-center space-y-6 lg:space-y-0 lg:space-x-8">
                {/* Official WEX Logo with enhanced styling */}
                <motion.div 
                  whileHover={{ scale: 1.05, rotate: 2 }}
                  className="flex items-center justify-center bg-white rounded-2xl p-6 shadow-xl border-2 border-wex-blue/20"
                >
                  <img 
                    src="https://www.wexinc.com/wp-content/uploads/2023/04/Logo.svg" 
                    alt="WEX Logo" 
                    className="h-16 w-auto object-contain"
                  />
                </motion.div>
                
                {/* Title and Subtitle with WEX colors */}
                <div className="text-center lg:text-left">
                  <motion.h1 
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.3 }}
                    className="text-4xl lg:text-5xl font-bold bg-gradient-to-r from-wex-blue via-wex-teal to-wex-red bg-clip-text text-transparent mb-3"
                  >
                    HSA Receipt Fraud Analyzer
                  </motion.h1>
                  <motion.p 
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.5 }}
                    className="text-lg text-gray-600 font-medium flex items-center justify-center lg:justify-start mb-3"
                  >
                    <span className="w-2 h-2 bg-wex-teal rounded-full mr-2 animate-pulse"></span>
                    Powered by WEX AI Technology
                  </motion.p>
                  <motion.div 
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.7 }}
                    className="flex flex-wrap items-center justify-center lg:justify-start gap-4 text-sm text-gray-500"
                  >
                    <span className="flex items-center bg-gradient-to-r from-wex-blue/10 to-wex-teal/10 px-3 py-1 rounded-full">
                      <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                      </svg>
                      Secure
                    </span>
                    <span className="flex items-center bg-gradient-to-r from-wex-teal/10 to-wex-yellow/10 px-3 py-1 rounded-full">
                      <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
                      </svg>
                      Real-time
                    </span>
                    <span className="flex items-center bg-gradient-to-r from-wex-yellow/10 to-wex-red/10 px-3 py-1 rounded-full">
                      <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      Accurate
                    </span>
                    <span className="flex items-center bg-gradient-to-r from-wex-red/10 to-wex-blue/10 px-3 py-1 rounded-full">
                      <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                      </svg>
                      AI-Powered
                    </span>
                  </motion.div>
                </div>
              </div>
            </div>
          </motion.div>
      
          {/* Enhanced Tab Navigation with WEX styling */}
          <motion.div 
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4 }}
            className="mb-8 flex justify-center"
          >
            <nav className="wex-nav flex space-x-2 p-2 rounded-2xl shadow-xl">
              <Button
                variant={activeTab === "claims" ? "primary" : "secondary"}
                size="md"
                onClick={() => setActiveTab("claims")}
                className={`flex items-center space-x-2 ${
                  activeTab === "claims" 
                    ? "bg-gradient-to-r from-wex-blue to-wex-teal text-white shadow-xl" 
                    : "bg-white/70 text-gray-600 hover:bg-white/90 border border-wex-blue/20"
                }`}
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 5H7a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                </svg>
                <span>Claims Management</span>
                {activeTab === "claims" && (
                  <div className="w-2 h-2 bg-white rounded-full animate-pulse"></div>
                )}
              </Button>
              <Button
                variant={activeTab === "admin" ? "primary" : "secondary"}
                size="md"
                onClick={() => setActiveTab("admin")}
                className={`flex items-center space-x-2 ${
                  activeTab === "admin" 
                    ? "bg-gradient-to-r from-wex-teal to-wex-red text-white shadow-xl" 
                    : "bg-white/70 text-gray-600 hover:bg-white/90 border border-wex-blue/20"
                }`}
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
                <span>Administrator</span>
                {resultVisible && activeTab !== "admin" && (
                  <span className="w-3 h-3 bg-wex-red rounded-full animate-pulse border-2 border-white"></span>
                )}
                {activeTab === "admin" && (
                  <div className="w-2 h-2 bg-white rounded-full animate-pulse"></div>
                )}
              </Button>
            </nav>
          </motion.div>

          {/* Claims Tab */}
          <AnimatePresence mode="wait">
            {activeTab === "claims" && (
              <motion.div
                key="claims"
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: 20 }}
                transition={{ duration: 0.3 }}
                className="w-full flex flex-col items-center"
              >
                {/* Claims Portfolio Header */}
                <div className="text-center mb-8">
                  <motion.h2 
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="text-3xl lg:text-4xl font-bold bg-gradient-to-r from-wex-blue to-wex-teal bg-clip-text text-transparent mb-4"
                  >
                    Claims Portfolio
                  </motion.h2>
                  <motion.p 
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.2 }}
                    className="text-gray-600 text-lg"
                  >
                    Monitor and manage HSA claims with real-time fraud detection
                  </motion.p>
                </div>

                {/* Search and Filter Controls with WEX styling */}
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.3 }}
                  className="mb-8 w-full max-w-7xl"
                >
                  <div className="glass-wex p-8 rounded-3xl shadow-2xl border border-wex-blue/20">
                    {/* Search Input */}
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
                          className="input-enhanced text-center border-wex-blue/30 focus:border-wex-blue focus:ring-wex-blue/20"
                        />
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                          <svg className="h-5 w-5 text-wex-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                          </svg>
                        </div>
                      </div>
                    </div>

                    {/* Filter and Sort Controls */}
                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                      <Select
                        label="Filter by Risk Level"
                        value={filterBy}
                        onChange={handleFilterChange}
                        options={[
                          { value: "all", label: "All Risk Levels" },
                          { value: "fraud", label: "High Risk (Fraud)" },
                          { value: "suspicious", label: "Medium Risk (Suspicious)" },
                          { value: "legit", label: "Low Risk (Legitimate)" }
                        ]}
                      />

                      <Select
                        label="Sort Claims By"
                        value={sortBy}
                        onChange={handleSortChange}
                        options={[
                          { value: "date", label: "Date (Newest First)" },
                          { value: "amount", label: "Amount (Highest First)" },
                          { value: "fraudScore", label: "Risk Score (Highest First)" }
                        ]}
                      />
                    </div>

                    {/* Results count with WEX styling */}
                    <div className="mt-6 text-center">
                      <div className="inline-flex items-center bg-gradient-to-r from-wex-blue/10 via-wex-teal/10 to-wex-blue/10 border border-wex-blue/30 text-wex-blue px-6 py-3 rounded-full shadow-lg text-base font-semibold">
                        Showing {filteredAndSortedClaims.length} of {claims.length} claims
                        {filteredAndSortedClaims.length !== claims.length && (
                          <span className="ml-2 bg-wex-blue/20 px-2 py-1 rounded-full text-xs text-wex-blue">
                            Filtered
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                </motion.div>

                {/* Claims Cards Grid */}
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.4 }}
                  className="w-full max-w-7xl mb-8"
                >
                  <div className="space-y-4">
                    <AnimatePresence>
                      {filteredAndSortedClaims.map((claim, index) => (
                        <ClaimCard 
                          key={`${claim.userId}-${claim.date}-${claim.amount}`} 
                          claim={claim} 
                          index={index} 
                        />
                      ))}
                    </AnimatePresence>
                  </div>
                </motion.div>
                
                {/* Add Claim Button with WEX styling */}
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.5 }}
                  className="flex justify-center mb-8"
                >
                  <button
                    onClick={toggleForm}
                    className="btn-wex flex items-center space-x-3 text-lg px-8 py-4 rounded-2xl shadow-2xl transform hover:scale-105 transition-all duration-300"
                  >
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                    </svg>
                    <span>Submit New Claim</span>
                    <div className="w-2 h-2 bg-white rounded-full opacity-60 animate-ping"></div>
                  </button>
                </motion.div>
                
                {/* Modern Claim Form */}
                <AnimatePresence>
                  {showForm && (
                    <ModernClaimForm
                      onSubmit={handleClaimSubmit}
                      onCancel={() => setShowForm(false)}
                      isSubmitting={isSubmitting}
                    />
                  )}
                </AnimatePresence>
              </motion.div>
            )}

            {/* Administrator Tab */}
            {activeTab === "admin" && (
              <motion.div
                key="admin"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{ duration: 0.3 }}
                className="w-full flex flex-col items-center"
              >
                <div className="w-full max-w-6xl">
                  <div className="text-center mb-8">
                    <motion.h2 
                      initial={{ opacity: 0, y: 20 }}
                      animate={{ opacity: 1, y: 0 }}
                      className="text-3xl lg:text-4xl font-bold bg-gradient-to-r from-wex-teal to-wex-red bg-clip-text text-transparent mb-4"
                    >
                      Administrator Dashboard
                    </motion.h2>
                    <motion.p 
                      initial={{ opacity: 0, y: 20 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: 0.2 }}
                      className="text-gray-600 text-lg"
                    >
                      Advanced fraud analysis and AI-powered claims investigation
                    </motion.p>
                  </div>
                  
                  {/* Show Analysis Result if available */}
                  <AnimatePresence>
                    {resultVisible && result && (
                      <motion.div
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: -20 }}
                        className="mb-8"
                      >
                        <div className="glass-wex p-8 rounded-3xl shadow-2xl border border-wex-blue/20">
                          <div className="flex items-center justify-between mb-6">
                            <h3 className="text-2xl font-bold text-wex-blue flex items-center">
                              Fraud Analysis Report
                              <span className="ml-3 text-sm bg-gradient-to-r from-wex-teal/20 to-wex-blue/20 text-wex-blue px-3 py-1 rounded-full border border-wex-blue/30">
                                Latest Analysis
                              </span>
                            </h3>
                            <Button 
                              variant="danger"
                              size="sm"
                              onClick={() => {
                                setResultVisible(false);
                                setActiveTab("claims"); // Volta para a aba Claims Management
                              }}
                              className="bg-gradient-to-r from-wex-red to-red-600"
                            >
                              ✕ Close
                            </Button>
                          </div>
                          
                          <div className="bg-white/70 rounded-xl p-6 shadow-inner border border-wex-blue/10">
                            <div 
                              className="prose prose-lg max-w-none text-gray-800"
                              style={{
                                lineHeight: '1.7',
                                fontFamily: '"Inter", "Segoe UI", system-ui, sans-serif'
                              }}
                              dangerouslySetInnerHTML={{
                                __html: result
                                  .replace(/## (.*)/g, '<h2 class="text-xl font-bold text-wex-blue mt-6 mb-3 pb-2 border-b border-wex-blue/20">$1</h2>')
                                  .replace(/### (.*)/g, '<h3 class="text-lg font-semibold text-wex-teal mt-4 mb-2">$1</h3>')
                                  .replace(/\*\*(.*?)\*\*/g, '<strong class="font-bold text-gray-900">$1</strong>')
                                  .replace(/• (.*)/g, '<li class="ml-4 mb-1 text-gray-700">$1</li>')
                                  .replace(/⚠ (.*)/g, '<div class="bg-wex-yellow/20 border-l-4 border-wex-yellow p-3 my-3"><span class="text-yellow-800 font-medium">⚠️ $1</span></div>')
                                  .replace(/Confidence Level: (.*)/g, '<div class="mt-4 p-3 bg-wex-blue/10 border border-wex-blue/30 rounded-lg"><strong class="text-wex-blue">🎯 Confidence Level:</strong> <span class="font-semibold text-wex-blue">$1</span></div>')
                                  .replace(/\n/g, '<br>')
                              }}
                            />
                          </div>
                          
                          <div className="mt-6 flex justify-center">
                            <div className="bg-gradient-to-r from-wex-blue/10 to-wex-teal/10 rounded-lg p-4 shadow-md border border-wex-blue/20">
                              <p className="text-sm text-wex-blue text-center font-medium flex items-center justify-center">
                                <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2-2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                                </svg>
                                Analysis powered by WEX AI Fraud Detection System
                              </p>
                            </div>
                          </div>
                        </div>
                      </motion.div>
                    )}
                  </AnimatePresence>
                  
                  {/* AI Claims Assistant Section */}
                  {!resultVisible && (
                    <motion.div
                      initial={{ opacity: 0, y: 20 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: 0.3 }}
                    >
                      <ModernChatAssistant />
                    </motion.div>
                  )}
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </div>
  );
}

export default StartPage
