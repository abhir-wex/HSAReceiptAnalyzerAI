import React, { useState } from "react";
import "./claimsDashboard.css";

const ClaimsDashboard = () => {
  const [results] = useState([
    {
      date: "2025-07-01",
      amount: "$100.00",
      merchant: "Sunrise Dental",
      description: "Dental Cleaning",
      riskScore: 0.15,
      status: "Clear",
    },
    {
      date: "2025-07-03",
      amount: "$1,200.00",
      merchant: "Luxury Wellness Spa",
      description: "Massage Therapy",
      riskScore: 0.85,
      status: "High Risk",
    },
  ]);

  return (
    <div className="dashboard">
      <header className="header">
        <h1>HSA Claims Analysis</h1>
      </header>

      {/* Summary cards */}
      <div className="summary-cards">
        <div className="card">
          <h2>Total Claims</h2>
          <p>{results.length}</p>
        </div>
        <div className="card">
          <h2>High Risk Claims</h2>
          <p>{results.filter(r => r.riskScore > 0.7).length}</p>
        </div>
        <div className="card">
          <h2>Average Risk Score</h2>
          <p>
            {(
              results.reduce((acc, r) => acc + r.riskScore, 0) / results.length
            ).toFixed(2)}
          </p>
        </div>
      </div>

      {/* Claims table */}
      <div className="claims-section">
        <h2>Claim Details</h2>
        <table className="claims-table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Merchant</th>
              <th>Description</th>
              <th>Amount</th>
              <th>Risk Score</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {results.map((claim, idx) => (
              <tr key={idx}>
                <td>{claim.date}</td>
                <td>{claim.merchant}</td>
                <td>{claim.description}</td>
                <td>{claim.amount}</td>
                <td>{(claim.riskScore * 100).toFixed(0)}%</td>
                <td
                  className={
                    claim.status === "High Risk" ? "status high" : "status clear"
                  }
                >
                  {claim.status}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default ClaimsDashboard;
