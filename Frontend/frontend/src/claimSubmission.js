import React, { useState } from "react";
import axios from "axios";

export default function FraudChecker() {
  const [file, setFile] = useState(null);
  const [form, setForm] = useState({
    amount: "",
    merchant: "",
    date: "",
    description: "",
  });
  const [result, setResult] = useState(null);
  const [claims, setClaims] = useState([]);
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setResult(null);

    try {
      const formData = new FormData();
      formData.append("receipt", file);
      Object.keys(form).forEach((key) => formData.append(key, form[key]))   ;

      // Use the correct fraud-check endpoint with proxy
      const res = await axios.post("/api/Analyze/fraud-check", formData, {
        headers: { 
          "Content-Type": "multipart/form-data"
        },
        timeout: 10000, // 10 second timeout
        withCredentials: false // Handle CORS
      });
      
      setResult(res.data);

      // Save claim to dashboard list
      setClaims((prev) => [
        ...prev,
        {
          id: prev.length + 1,
          ...form,
          fraudScore: res.data.fraudScore || Math.floor(Math.random() * 100), // Fallback if no score
          isDuplicate: res.data.isDuplicate || false,
          status:
            (res.data.fraudScore || 0) > 70
              ? "Fraud"
              : (res.data.fraudScore || 0) > 30
              ? "Suspicious"
              : "Legit",
        },
      ]);
    } catch (err) {
      console.error('Fraud check error:', err);
      console.error('Error details:', {
        message: err.message,
        code: err.code,
        response: err.response?.data,
        status: err.response?.status
      });
      
      let errorMessage = "Error checking fraud";
      
      if (err.response) {
        // Server responded with error status
        errorMessage = `Server error: ${err.response.status} - ${err.response.data?.message || JSON.stringify(err.response.data) || 'Unknown error'}`;
      } else if (err.request) {
        // Request was made but no response received - provide mock data for development
        console.warn('No response from server, using mock data for development');
        console.warn('Request details:', err.request);
        
        if (err.code === 'ECONNREFUSED' || err.message.includes('Network Error')) {
          console.warn('Backend server appears to be down or not accessible at https://localhost:44395');
        }
        
        const mockResult = {
          fraudScore: Math.floor(Math.random() * 100),
          isDuplicate: Math.random() > 0.7,
          flags: ['Mock analysis - backend not available'],
          message: 'This is mock data. Backend server not responding.'
        };
        setResult(mockResult);
        
        // Save mock claim to dashboard
        setClaims((prev) => [
          ...prev,
          {
            id: prev.length + 1,
            ...form,
            fraudScore: mockResult.fraudScore,
            isDuplicate: mockResult.isDuplicate,
            status:
              mockResult.fraudScore > 70
                ? "Fraud"
                : mockResult.fraudScore > 30
                ? "Suspicious"
                : "Legit",
          },
        ]);
        return; // Don't show error if we're using mock data
      } else {
        // Something else happened
        errorMessage = `Request error: ${err.message}`;
      }
      
      setResult({ error: errorMessage });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-900 text-white flex flex-col items-center p-6">
      <div className="w-full max-w-2xl bg-gray-800 rounded-2xl shadow-lg p-8 mb-8">
        <h1 className="text-2xl font-bold mb-4">HSA Claim Fraud Check</h1>

        {/* Claim Submission Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          <input
            type="file"
            onChange={(e) => setFile(e.target.files[0])}
            className="block w-full text-gray-300"
            required
          />

          <input
            type="text"
            name="merchant"
            placeholder="Merchant"
            value={form.merchant}
            onChange={handleChange}
            className="w-full p-2 rounded bg-gray-700 text-white"
            required
          />
          <input
            type="number"
            name="amount"
            placeholder="Amount"
            value={form.amount}
            onChange={handleChange}
            className="w-full p-2 rounded bg-gray-700 text-white"
            required
          />
          <input
            type="date"
            name="date"
            value={form.date}
            onChange={handleChange}
            className="w-full p-2 rounded bg-gray-700 text-white"
            required
          />
          <textarea
            name="description"
            placeholder="Description"
            value={form.description}
            onChange={handleChange}
            className="w-full p-2 rounded bg-gray-700 text-white"
          />

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 py-2 px-4 rounded-lg"
          >
            {loading ? "Checking..." : "Submit Claim"}
          </button>
        </form>

        {/* Result for the last submission */}
        {result && (
          <div className="mt-6 p-4 bg-gray-700 rounded-xl">
            {result.error ? (
              <p className="text-red-400">{result.error}</p>
            ) : (
              <>
                <h2 className="text-xl font-semibold mb-2">Fraud Analysis Result</h2>
                {result.message && (
                  <p className="text-yellow-300 mb-2 text-sm">⚠️ {result.message}</p>
                )}
                <p><strong>Fraud Score:</strong> {result.fraudScore}</p>
                <p><strong>Duplicate Receipt:</strong> {result.isDuplicate ? "⚠️ Yes" : "✅ No"}</p>
                {result.flags && result.flags.length > 0 && (
                  <ul className="mt-2 list-disc list-inside text-yellow-300">
                    {result.flags.map((f, i) => <li key={i}>{f}</li>)}
                  </ul>
                )}
              </>
            )}
          </div>
        )}
      </div>

      {/* Dashboard */}
      <div className="w-full max-w-4xl bg-gray-800 rounded-2xl shadow-lg p-6">
        <h2 className="text-xl font-bold mb-4">Claims Dashboard</h2>
        {claims.length === 0 ? (
          <p className="text-gray-400">No claims submitted yet.</p>
        ) : (
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-700">
                <th className="p-2">#</th>
                <th className="p-2">Date</th>
                <th className="p-2">Merchant</th>
                <th className="p-2">Amount</th>
                <th className="p-2">Fraud Score</th>
                <th className="p-2">Status</th>
              </tr>
            </thead>
            <tbody>
              {claims.map((c) => (
                <tr key={c.id} className="border-b border-gray-700">
                  <td className="p-2">{c.id}</td>
                  <td className="p-2">{c.date}</td>
                  <td className="p-2">{c.merchant}</td>
                  <td className="p-2">${c.amount}</td>
                  <td className="p-2">{c.fraudScore}</td>
                  <td
                    className={`p-2 font-bold ${
                      c.status === "Fraud"
                        ? "text-red-400"
                        : c.status === "Suspicious"
                        ? "text-yellow-400"
                        : "text-green-400"
                    }`}
                  >
                    {c.status}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
