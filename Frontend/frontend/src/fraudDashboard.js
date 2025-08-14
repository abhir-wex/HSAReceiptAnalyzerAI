import React from "react";
import "./fraudDashboard.css";
import AIClaimAssistant from "./aiAssistant";

const FraudDashboard = () => {
  return (
    <div className="fraud-dashboard">
      <header className="header">
        <div className="header-content">
          <img 
            src="https://www.wexinc.com/wp-content/uploads/2023/04/Logo.svg" 
            alt="WEX Logo" 
            className="wex-logo" 
          />
          <h1>Fraud Analysis</h1>
        </div>
      </header>

      <div className="dashboard-grid">
        {/* AI Assistant Chat section */}
        <div className="chat-card">
          <AIClaimAssistant />
        </div>

        {/* Summary section */}
        <div className="summary-card">
          <div className="summary-item">
            <strong>320</strong>
            <span>Total claims</span>
          </div>
          <div className="summary-item">
            <strong>$51,600</strong>
            <span>Total amount</span>
          </div>
          <div className="summary-item">
            <strong>16.3%</strong>
            <span>Suspected fraud</span>
          </div>
          <button className="export-btn">Export</button>
        </div>

        {/* Sentiment */}
        <div className="sentiment-card">
          <h3>Sentiment Analysis</h3>
          <div className="donut-chart">
            <svg width="100" height="100">
              <circle
                cx="50"
                cy="50"
                r="40"
                stroke="#ddd"
                strokeWidth="10"
                fill="none"
              />
              <circle
                cx="50"
                cy="50"
                r="40"
                stroke="#4a90e2"
                strokeWidth="10"
                fill="none"
                strokeDasharray={`${2 * Math.PI * 40 * 0.6} ${
                  2 * Math.PI * 40 * 0.4
                }`}
                strokeDashoffset="0"
                transform="rotate(-90 50 50)"
              />
            </svg>
            <div className="donut-text">60%</div>
          </div>
          <ul className="sentiment-legend">
            <li><span className="dot blue"></span>Positive 60%</li>
            <li><span className="dot gray"></span>Neutral 6%</li>
            <li><span className="dot red"></span>Negative 2%</li>
          </ul>
        </div>

        {/* Usage over time */}
        <div className="usage-card">
          <h3>Usage Over Time</h3>
          <svg width="200" height="100">
            <polyline
              fill="none"
              stroke="#4a90e2"
              strokeWidth="3"
              points="0,50 40,30 80,60 120,20 160,40 200,30"
            />
          </svg>
        </div>

        {/* Top Questions */}
        <div className="questions-card">
          <h3>Top Questions</h3>
          <ul>
            <li>How many fraud cases are there?</li>
            <li>Generate summary of recent claims.</li>
            <li>Show me suspicious activity.</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default FraudDashboard;
