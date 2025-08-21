import React, { useState, useEffect } from "react";
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

// Pagination Component
const PaginationControls = ({ pagination, onPageChange, isLoading }) => {
  const { currentPage, totalPages, hasNextPage, hasPreviousPage, totalClaims } = pagination;

  const getPageNumbers = () => {
    const pages = [];
    const showPages = 5; // Show 5 page numbers at most
    
    let startPage = Math.max(1, currentPage - Math.floor(showPages / 2));
    let endPage = Math.min(totalPages, startPage + showPages - 1);
    
    // Adjust start page if we're near the end
    if (endPage - startPage + 1 < showPages) {
      startPage = Math.max(1, endPage - showPages + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    
    return pages;
  };

  if (totalPages <= 1) return null;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex flex-col items-center space-y-4 py-6"
    >
      {/* Page Info */}
      <div className="text-sm text-gray-600 bg-white/70 px-4 py-2 rounded-full border border-wex-blue/20">
        Showing page {currentPage} of {totalPages} ({totalClaims} total claims)
      </div>

      {/* Pagination Controls */}
      <div className="flex items-center space-x-2">
        {/* Previous Button */}
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={!hasPreviousPage || isLoading}
          className="px-4 py-2 bg-gradient-to-r from-wex-blue/10 to-wex-teal/10 border border-wex-blue/30 text-wex-blue rounded-lg hover:bg-wex-blue/20 transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 19l-7-7 7-7" />
          </svg>
          <span>Previous</span>
        </button>

        {/* Page Numbers */}
        <div className="flex items-center space-x-1">
          {currentPage > 3 && (
            <>
              <button
                onClick={() => onPageChange(1)}
                disabled={isLoading}
                className="px-3 py-2 text-wex-blue hover:bg-wex-blue/10 rounded-lg transition-all duration-300 disabled:opacity-50"
              >
                1
              </button>
              {currentPage > 4 && (
                <span className="px-2 py-2 text-gray-400">...</span>
              )}
            </>
          )}

          {getPageNumbers().map((pageNum) => (
            <button
              key={pageNum}
              onClick={() => onPageChange(pageNum)}
              disabled={isLoading}
              className={`px-3 py-2 rounded-lg transition-all duration-300 disabled:opacity-50 ${
                pageNum === currentPage
                  ? 'bg-gradient-to-r from-wex-blue to-wex-teal text-white shadow-lg'
                  : 'text-wex-blue hover:bg-wex-blue/10'
              }`}
            >
              {pageNum}
            </button>
          ))}

          {currentPage < totalPages - 2 && (
            <>
              {currentPage < totalPages - 3 && (
                <span className="px-2 py-2 text-gray-400">...</span>
              )}
              <button
                onClick={() => onPageChange(totalPages)}
                disabled={isLoading}
                className="px-3 py-2 text-wex-blue hover:bg-wex-blue/10 rounded-lg transition-all duration-300 disabled:opacity-50"
              >
                {totalPages}
              </button>
            </>
          )}
        </div>

        {/* Next Button */}
        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={!hasNextPage || isLoading}
          className="px-4 py-2 bg-gradient-to-r from-wex-teal/10 to-wex-blue/10 border border-wex-teal/30 text-wex-teal rounded-lg hover:bg-wex-teal/20 transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-2"
        >
          <span>Next</span>
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>

      {/* Quick Jump */}
      <div className="flex items-center space-x-2 text-sm">
        <span className="text-gray-600">Jump to page:</span>
        <select
          value={currentPage}
          onChange={(e) => onPageChange(parseInt(e.target.value))}
          disabled={isLoading}
          className="px-2 py-1 border border-wex-blue/30 rounded text-wex-blue focus:border-wex-blue focus:outline-none disabled:opacity-50"
        >
          {Array.from({ length: totalPages }, (_, i) => i + 1).map(pageNum => (
            <option key={pageNum} value={pageNum}>
              {pageNum}
            </option>
          ))}
        </select>
      </div>
    </motion.div>
  );
};

// Initial fallback claims data (used only if backend is unavailable)
const fallbackClaims = [
  {
    id: "fallback-1",
    userId: "USR001",
    date: "2025-07-01",
    amount: "$100.00",
    merchant: "Sunrise Dental",
    description: "Dental Cleaning",
    fraudScore: 15,
    status: "Legit"
  },
  {
    id: "fallback-2",
    userId: "USR002",
    date: "2025-07-10",
    amount: "$250.00",
    merchant: "FitZone Gym",
    description: "Annual Gym",
    fraudScore: 72,
    status: "Fraud"
  },
  {
    id: "fallback-3",
    userId: "USR001",
    date: "2025-07-15",
    amount: "$45.99",
    merchant: "HealthMart Pharmacy",
    description: "Prescription Medicine",
    fraudScore: 8,
    status: "Legit"
  },
  {
    id: "fallback-4",
    userId: "USR003",
    date: "2025-07-18",
    amount: "$150.00",
    merchant: "QuickCare Clinic",
    description: "Urgent Care Visit",
    fraudScore: 55,
    status: "Suspicious"
  },
  {
    id: "fallback-5",
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
  const [claims, setClaims] = useState([]);
  const [pagination, setPagination] = useState({
    currentPage: 1,
    pageSize: 5,
    totalClaims: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false
  });
  const [isLoadingClaims, setIsLoadingClaims] = useState(true);
  const [claimsError, setClaimsError] = useState(null);
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

  // Load claims from backend on component mount
  useEffect(() => {
    loadClaimsFromBackend();
  }, []);

  // Reload claims when pagination, search, filter, or sort changes
  useEffect(() => {
    const debounceTimer = setTimeout(() => {
      loadClaimsFromBackend(pagination.currentPage, searchTerm, filterBy, sortBy);
    }, 500); // Debounce search

    return () => clearTimeout(debounceTimer);
  }, [pagination.currentPage, searchTerm, filterBy, sortBy]);

  // Function to load claims from backend with pagination
  const loadClaimsFromBackend = async (page = 1, search = searchTerm, filter = filterBy, sort = sortBy) => {
    try {
      setIsLoadingClaims(true);
      setClaimsError(null);
      
      console.log(`Loading claims from backend - Page: ${page}, Search: "${search}", Filter: ${filter}, Sort: ${sort}`);
      
      const queryParams = new URLSearchParams({
        page: page.toString(),
        pageSize: '5',
        search: search,
        filter: filter,
        sortBy: sort
      });

      const response = await fetch(`/api/ClaimDatabase/claims?${queryParams}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const responseData = await response.json();
        console.log('Successfully loaded claims from backend:', responseData);
        
        // Format claims to ensure consistent structure
        const formattedClaims = responseData.claims.map((claim, index) => ({
          id: claim.id || `backend-${index}`,
          userId: claim.userId || 'Unknown',
          date: claim.date || new Date().toISOString().split('T')[0],
          amount: claim.amount || '$0.00',
          merchant: claim.merchant || 'Unknown Merchant',
          description: claim.description || 'No description',
          fraudScore: claim.fraudScore || 0,
          status: claim.status || 'Unknown',
          submissionDate: claim.submissionDate || new Date().toISOString(),
          isFraudulent: claim.isFraudulent || false
        }));
        
        setClaims(formattedClaims);
        setPagination(responseData.pagination);
      } else {
        console.error('Failed to load claims from backend. Status:', response.status);
        const errorText = await response.text();
        console.error('Error response:', errorText);
        
        setClaimsError(`Failed to load claims: ${response.status} ${response.statusText}`);
        
        // Use fallback data with simulated pagination
        const fallbackPagination = {
          currentPage: 1,
          pageSize: 5,
          totalClaims: fallbackClaims.length,
          totalPages: 1,
          hasNextPage: false,
          hasPreviousPage: false
        };
        setClaims(fallbackClaims);
        setPagination(fallbackPagination);
      }
    } catch (error) {
      console.error('Error loading claims from backend:', error);
      setClaimsError(`Network error: ${error.message}`);
      
      // Use fallback data with simulated pagination
      const fallbackPagination = {
        currentPage: 1,
        pageSize: 5,
        totalClaims: fallbackClaims.length,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false
      };
      setClaims(fallbackClaims);
      setPagination(fallbackPagination);
    } finally {
      setIsLoadingClaims(false);
    }
  };

  // Function to handle page changes
  const handlePageChange = (newPage) => {
    if (newPage >= 1 && newPage <= pagination.totalPages && !isLoadingClaims) {
      setPagination(prev => ({ ...prev, currentPage: newPage }));
    }
  };

  // Function to refresh claims (useful after submitting new claims)
  const refreshClaims = async () => {
    await loadClaimsFromBackend(1, searchTerm, filterBy, sortBy); // Reset to page 1 when refreshing
    setPagination(prev => ({ ...prev, currentPage: 1 }));
  };

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

  // Enhanced search handler (pagination will handle the actual search)
  const handleSearchChange = (e) => {
    const value = e.target.value;
    setSearchTerm(value);
    // Reset to page 1 when searching
    setPagination(prev => ({ ...prev, currentPage: 1 }));
  };

  // Enhanced filter handler (pagination will handle the actual filter)
  const handleFilterChange = (e) => {
    const value = e.target.value;
    setFilterBy(value);
    // Reset to page 1 when filtering
    setPagination(prev => ({ ...prev, currentPage: 1 }));
  };

  // Enhanced sort handler (pagination will handle the actual sort)
  const handleSortChange = (e) => {
    const value = e.target.value;
    setSortBy(value);
    // Reset to page 1 when sorting
    setPagination(prev => ({ ...prev, currentPage: 1 }));
  };

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
      
      // Save claim to backend database
      try {
        console.log("Saving claim to backend database...");
        
        const claimPayload = {
          userId: userId,
          name: userId, // Using userId as name for now
          merchant: merchant,
          serviceType: "Healthcare",
          amount: parseFloat(amount),
          dateOfService: new Date(date).toISOString(),
          category: "Medical",
          description: description,
          fraudScore: fraudScore,
          userAge: 30, // Default age
          items: [description],
          ipAddress: "127.0.0.1", // Default IP
          receiptHash: `hash-${Date.now()}` // Generate simple hash
        };

        const saveResponse = await fetch('/api/ClaimDatabase/claims', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(claimPayload)
        });

        if (saveResponse.ok) {
          const savedClaim = await saveResponse.json();
          console.log("Claim saved to database successfully:", savedClaim);
          
          // Refresh claims from backend to get the actual stored data
          setTimeout(() => {
            refreshClaims();
          }, 1000);
        } else {
          console.error("Failed to save claim to database:", saveResponse.status);
        }
      } catch (saveError) {
        console.error("Error saving claim to database:", saveError);
      }
      
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
                  
                  {/* Data source indicator */}
                  <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.3 }}
                    className="mt-4 flex justify-center"
                  >
                    <div className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium ${
                      claimsError 
                        ? 'bg-wex-red/10 text-wex-red border border-wex-red/30'
                        : isLoadingClaims
                        ? 'bg-wex-yellow/10 text-wex-yellow border border-wex-yellow/30'
                        : 'bg-wex-blue/10 text-wex-blue border border-wex-blue/30'
                    }`}>
                      {claimsError ? (
                        <>
                          <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
                          </svg>
                          Using Fallback Data
                        </>
                      ) : isLoadingClaims ? (
                        <>
                          <svg className="w-4 h-4 mr-2 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                          </svg>
                          Loading from Database
                        </>
                      ) : (
                        <>
                          <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
                          </svg>
                          Live Database Connection (Paginated)
                        </>
                      )}
                    </div>
                  </motion.div>
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
                        Page {pagination.currentPage} - Showing {claims.length} of {pagination.totalClaims} claims
                        {(searchTerm || filterBy !== "all") && (
                          <span className="ml-2 bg-wex-blue/20 px-2 py-1 rounded-full text-xs text-wex-blue">
                            Filtered
                          </span>
                        )}
                      </div>
                      
                      {/* Refresh button */}
                      <div className="mt-4 flex justify-center">
                        <button
                          onClick={refreshClaims}
                          disabled={isLoadingClaims}
                          className="inline-flex items-center px-4 py-2 bg-gradient-to-r from-wex-teal/10 to-wex-blue/10 border border-wex-teal/30 text-wex-teal rounded-lg hover:bg-wex-teal/20 transition-all duration-300 disabled:opacity-50"
                        >
                          <svg className={`w-4 h-4 mr-2 ${isLoadingClaims ? 'animate-spin' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                          </svg>
                          {isLoadingClaims ? 'Refreshing...' : 'Refresh Claims'}
                        </button>
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
                  {isLoadingClaims ? (
                    <div className="text-center py-12">
                      <div className="inline-flex items-center text-wex-blue">
                        <svg className="w-8 h-8 mr-3 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                        </svg>
                        <span className="text-lg font-medium">Loading Claims from Database...</span>
                      </div>
                    </div>
                  ) : claims.length === 0 ? (
                    <div className="text-center py-12">
                      <div className="inline-flex flex-col items-center text-gray-500">
                        <svg className="w-16 h-16 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                        </svg>
                        <span className="text-lg font-medium">No claims found</span>
                        <span className="text-sm">Try adjusting your search or filter criteria</span>
                      </div>
                    </div>
                  ) : (
                    <div className="space-y-4">
                      <AnimatePresence>
                        {claims.map((claim, index) => (
                          <ClaimCard 
                            key={claim.id || `${claim.userId}-${claim.date}-${claim.amount}`} 
                            claim={claim} 
                            index={index} 
                          />
                        ))}
                      </AnimatePresence>
                    </div>
                  )}
                </motion.div>

                {/* Pagination Controls */}
                <PaginationControls 
                  pagination={pagination}
                  onPageChange={handlePageChange}
                  isLoading={isLoadingClaims}
                />
                
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
