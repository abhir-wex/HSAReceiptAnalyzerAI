import React, { useState } from "react";

const initialClaims = [
  {
    date: "2025-07-01",
    amount: "$100.00",
    merchant: "Sunrise Dental",
    description: "Dental Cleaning",
  },
  {
    date: "2025-07-10",
    amount: "$250.00",
    merchant: "FitZone Gym",
    description: "Annual Gym",
  },
];

export default function StartPage() {
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

  const toggleForm = () => setShowForm((v) => !v);

  const handleAdminSubmit = async (e) => {
    e.preventDefault();
    if (!adminPrompt.trim()) return;

    setAdminLoading(true);
    
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
    }
  };

  const handlePredefinedQuestion = (question) => {
    handleAdminSubmit(question.question);
  };

  const clearChat = () => {
    setChatHistory([]);
    setAdminResponse("");
    setAdminPrompt("");
    setCurrentQuestionIndex(0);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);
    try {
      const response = await fetch("/Analyze/upload", {
        method: "POST",
        body: formData,
      });
      const resultText = await response.text();
      setResult(resultText);
      setResultVisible(true);
      // Optionally add the new claim to the table
      setClaims([
        ...claims,
        {
          date: form.date.value,
          amount: `$${parseFloat(form.amount.value).toFixed(2)}`,
          merchant: form.merchant.value,
          description: form.description.value,
        },
      ]);
      form.reset();
      setShowForm(false);
    } catch (err) {
      setResult("Error analyzing claim.");
      setResultVisible(true);
    }
  };

  return (
    <div className="bg-gray-100 text-gray-900 min-h-screen flex flex-col items-center justify-start py-8">
      <div className="w-full max-w-4xl mx-auto px-8">
        {/* Header with WEX Logo */}
        <div className="flex items-center justify-center mb-8 bg-white rounded-lg shadow-sm p-6">
          <div className="flex items-center space-x-4">
            {/* Official WEX Logo */}
            <div className="flex items-center justify-center bg-white rounded-lg p-2">
              <img 
                src="https://www.wexinc.com/wp-content/uploads/2023/04/Logo.svg" 
                alt="WEX Logo" 
                className="h-12 w-auto object-contain"
              />
            </div>
            
            {/* Title and Subtitle */}
            <div className="text-left">
              <h1 className="text-2xl font-bold text-gray-800 mb-1">HSA Receipt Fraud Analyzer</h1>
              <p className="text-sm text-gray-600 font-medium">Powered by WEX Technology</p>
            </div>
          </div>
        </div>
      
      {/* Tab Navigation */}
      <div className="mb-6 flex justify-center">
        <nav className="flex space-x-4">
          <button
            onClick={() => setActiveTab("claims")}
            className={`px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === "claims"
                ? "bg-blue-600 text-white"
                : "bg-white text-gray-700 hover:bg-gray-100"
            }`}
          >
            📋 Claims Management
          </button>
          <button
            onClick={() => setActiveTab("admin")}
            className={`px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === "admin"
                ? "bg-blue-600 text-white"
                : "bg-white text-gray-700 hover:bg-gray-100"
            }`}
          >
            👨‍💼 Administrator
          </button>
        </nav>
      </div>

      {/* Claims Tab */}
      {activeTab === "claims" && (
        <div className="w-full flex flex-col items-center">
          {/* Claims Table */}
          <div className="mb-6 w-full max-w-4xl">
            <h2 className="text-lg font-semibold mb-2 text-center">📋 Existing Claims</h2>
            <div className="overflow-x-auto flex justify-center">
              <table className="w-full bg-white shadow rounded">
                <thead className="bg-gray-200 text-gray-700">
                  <tr>
                    <th className="py-2 px-4 text-center">Date</th>
                    <th className="py-2 px-4 text-center">Amount</th>
                    <th className="py-2 px-4 text-center">Merchant</th>
                    <th className="py-2 px-4 text-center">Description</th>
                  </tr>
                </thead>
                <tbody>
                  {claims.map((claim, idx) => (
                    <tr
                      key={idx}
                      className={
                        "border-b" + (idx % 2 === 1 ? " bg-gray-50" : "")
                      }
                    >
                      <td className="py-2 px-4 text-center">{claim.date}</td>
                      <td className="py-2 px-4 text-center">{claim.amount}</td>
                      <td className="py-2 px-4 text-center">{claim.merchant}</td>
                      <td className="py-2 px-4 text-center">{claim.description}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          
          {/* Add Claim Button */}
          <div className="flex justify-center mb-4">
            <button
              onClick={toggleForm}
              className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
            >
              ➕ Add New Claim
            </button>
          </div>
          
          {/* Claim Form */}
          {showForm && (
            <div className="flex justify-center w-full">
              <form
                id="claimForm"
                className="bg-white p-6 shadow rounded mb-4 w-full max-w-2xl"
                onSubmit={handleSubmit}
              >
                <h2 className="text-lg font-semibold mb-4 text-center">📝 Submit New Claim</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                <input
                  name="date"
                  type="date"
                  required
                  className="border p-2 rounded text-center"
                  placeholder="Date of Service"
                />
                <input
                  name="amount"
                  type="number"
                  required
                  className="border p-2 rounded text-center"
                  placeholder="Amount ($)"
                />
                <input
                  name="merchant"
                  type="text"
                  required
                  className="border p-2 rounded text-center"
                  placeholder="Merchant Name"
                />
                <input
                  name="description"
                  type="text"
                  required
                  className="border p-2 rounded text-center"
                  placeholder="Description"
                />
              </div>
              {/* Receipt Upload */}
              <div className="mb-4 text-center">
                <label className="block mb-1 font-medium text-center">🧾 Upload Receipt</label>
                <input
                  name="image"
                  type="file"
                  accept=".pdf,.jpg,.png"
                  className="border p-2 rounded w-full"
                />
              </div>
              {/* Optional Prompt */}
              <div className="mb-4 text-center">
                <label className="block mb-1 font-medium text-center">
                  🧠 Optional Custom Prompt
                </label>
                <textarea
                  name="customPrompt"
                  rows="3"
                  className="border p-2 rounded w-full text-center"
                  placeholder="User customized prompt"
                ></textarea>
              </div>                <button
                  type="submit"
                  className="bg-green-600 text-white px-4 py-2 rounded hover:bg-green-700 w-full"
                >
                  📤 Submit Claim 
                </button>
              </form>
            </div>
          )}
          
          {/* Analysis Result */}
          {resultVisible && (
            <div className="flex justify-center w-full">
              <div className="mt-6 p-4 bg-yellow-100 border-l-4 border-yellow-500 text-yellow-800 w-full max-w-2xl text-center">
                {result}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Administrator Tab */}
      {activeTab === "admin" && (
        <div className="space-y-6 w-full flex flex-col items-center">
          <h2 className="text-lg font-semibold mb-4 text-center">👨‍💼 Administrator Panel</h2>
          <p className="text-gray-600 mb-6 text-center">
            Use predefined prompts or enter your custom analysis prompt for administrative fraud detection.
          </p>
          
          {/* Predefined Prompts Section */}
          <div className="flex justify-center w-full mb-6">
            <div className="bg-white p-6 shadow rounded w-full max-w-2xl">
              <h3 className="text-md font-semibold mb-4 text-center">📋 Quick Analysis Options</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {Object.entries(predefinedPrompts).map(([key, prompts]) => (
                  <div key={key} className="space-y-2">
                    <h4 className="font-medium text-sm text-gray-700">{key.replace(/([A-Z])/g, ' $1').trim()}</h4>
                    {prompts.map((prompt, index) => (
                      <button
                        key={index}
                        onClick={() => {
                          setAdminPrompt(prompt);
                          handleAdminSubmit({ preventDefault: () => {} });
                        }}
                        className="w-full text-left p-2 text-sm bg-gray-50 hover:bg-blue-50 border border-gray-200 rounded transition-colors"
                      >
                        {prompt}
                      </button>
                    ))}
                  </div>
                ))}
              </div>
            </div>
          </div>
          
          {/* Custom Prompt Section */}
          <div className="flex justify-center w-full">
            <div className="bg-white p-6 shadow rounded w-full max-w-2xl">
              <form onSubmit={handleAdminSubmit}>
                <div className="mb-4">
                  <label className="block mb-2 font-medium text-center">🔍 Custom Admin Analysis Prompt</label>
                  <textarea
                    value={adminPrompt}
                    onChange={(e) => setAdminPrompt(e.target.value)}
                    className="w-full border p-3 rounded h-32 resize-none"
                    placeholder="Enter your custom fraud analysis query here..."
                  />
                </div>
                <div className="text-center space-x-3">
                  <button
                    type="submit"
                    disabled={adminLoading}
                    className="bg-purple-600 text-white px-6 py-2 rounded hover:bg-purple-700 disabled:opacity-50"
                  >
                    {adminLoading ? "🔄 Analyzing..." : "🔍 Analyze"}
                  </button>
                  <button
                    type="button"
                    onClick={clearChat}
                    className="bg-gray-500 text-white px-6 py-2 rounded hover:bg-gray-600"
                  >
                    🗑️ Clear
                  </button>
                </div>
              </form>
              
              {/* Admin Response */}
              {adminResponse && (
                <div className="mt-6 p-4 bg-purple-100 border-l-4 border-purple-500 text-purple-800">
                  <h3 className="font-semibold mb-2">Analysis Result:</h3>
                  <p className="whitespace-pre-wrap">{adminResponse}</p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
      </div>
    </div>
  );
}
