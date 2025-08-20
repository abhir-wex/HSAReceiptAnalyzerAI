import React from 'react';
import { motion } from 'framer-motion';
import { StatusIndicator } from './UIComponents';

const FraudScoreMeter = ({ score }) => {
  const getColor = (score) => {
    if (score <= 30) return "#22c55e"; // Green
    if (score <= 70) return "#FFB81C"; // WEX Yellow
    return "#C8102E"; // WEX Red
  };

  const getStatus = (score) => {
    if (score <= 30) return "Legitimate";
    if (score <= 70) return "Suspicious";
    return "Fraud";
  };

  // Convert score (0-100) to angle (-90 to 90 degrees)
  const angle = (score / 100) * 180 - 90;
  
  return (
    <div className="flex flex-col items-center">
      {/* Speedometer */}
      <div className="relative w-20 h-10 mb-2">
        <svg 
          width="80" 
          height="40" 
          viewBox="0 0 80 40" 
          className="transform"
        >
          {/* Background arc */}
          <path
            d="M 8 32 A 24 24 0 0 1 72 32"
            fill="none"
            stroke="#e5e7eb"
            strokeWidth="6"
            strokeLinecap="round"
          />
          
          {/* Green section (0-30%) */}
          <path
            d="M 8 32 A 24 24 0 0 1 29.6 14.4"
            fill="none"
            stroke="#22c55e"
            strokeWidth="6"
            strokeLinecap="round"
            opacity="0.7"
          />
          
          {/* WEX Yellow section (30-70%) */}
          <path
            d="M 29.6 14.4 A 24 24 0 0 1 50.4 14.4"
            fill="none"
            stroke="#FFB81C"
            strokeWidth="6"
            strokeLinecap="round"
            opacity="0.7"
          />
          
          {/* WEX Red section (70-100%) */}
          <path
            d="M 50.4 14.4 A 24 24 0 0 1 72 32"
            fill="none"
            stroke="#C8102E"
            strokeWidth="6"
            strokeLinecap="round"
            opacity="0.7"
          />
          
          {/* Center dot */}
          <circle cx="40" cy="32" r="2" fill="#374151" />
          
          {/* Animated arrow pointer */}
          <g transform={`rotate(${angle} 40 32)`}>
            <motion.line
              initial={{ pathLength: 0 }}
              animate={{ pathLength: 1 }}
              transition={{ duration: 1, delay: 0.5 }}
              x1="40"
              y1="32"
              x2="40"
              y2="16"
              stroke={getColor(score)}
              strokeWidth="2"
              strokeLinecap="round"
              style={{
                filter: `drop-shadow(0 2px 4px ${getColor(score)}40)`
              }}
            />
            {/* Arrow tip */}
            <polygon
              points="40,14 38,18 42,18"
              fill={getColor(score)}
            />
          </g>
        </svg>
      </div>
      
      {/* Score display */}
      <div className="text-center">
        <div className="text-lg font-bold" style={{ color: getColor(score) }}>
          {score}%
        </div>
        <div className="text-xs text-gray-500">
          {getStatus(score)}
        </div>
      </div>
    </div>
  );
};

const ClaimCard = ({ claim, index }) => {
  const getStatusVariant = (score) => {
    if (score <= 30) return 'legitimate';
    if (score <= 70) return 'suspicious';
    return 'fraud';
  };

  const getStatusColor = (score) => {
    if (score <= 30) return 'text-emerald-600';
    if (score <= 70) return 'text-wex-yellow';
    return 'text-wex-red';
  };

  const getAmountColor = (amount) => {
    const numAmount = parseFloat(amount.replace('$', ''));
    if (numAmount > 200) return 'text-wex-red';
    if (numAmount > 100) return 'text-wex-blue';
    return 'text-wex-teal';
  };

  const getBorderColor = (score) => {
    if (score <= 30) return 'border-l-emerald-500';
    if (score <= 70) return 'border-l-wex-yellow';
    return 'border-l-wex-red';
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.1, duration: 0.3 }}
      whileHover={{ 
        scale: 1.02,
        boxShadow: "0 20px 40px rgba(74, 144, 226, 0.15)"
      }}
    >
      <div className={`glass-wex p-6 rounded-2xl border-l-4 ${getBorderColor(claim.fraudScore)} shadow-xl hover:shadow-2xl transition-all duration-300`}>
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 items-center">
          {/* User Info with WEX styling */}
          <div className="lg:col-span-2">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-gradient-to-br from-wex-blue to-wex-teal rounded-full flex items-center justify-center text-white font-bold text-sm shadow-md">
                {claim.userId.slice(-2)}
              </div>
              <div>
                <div className="font-semibold text-gray-900">{claim.userId}</div>
                <div className="text-xs text-wex-blue font-medium">User ID</div>
              </div>
            </div>
          </div>

          {/* Date */}
          <div className="lg:col-span-2">
            <div className="text-center lg:text-left">
              <div className="font-semibold text-gray-900">
                {new Date(claim.date).toLocaleDateString('en-US', {
                  month: 'short',
                  day: 'numeric',
                  year: 'numeric'
                })}
              </div>
              <div className="text-xs text-wex-teal font-medium">Service Date</div>
            </div>
          </div>

          {/* Amount with WEX colors */}
          <div className="lg:col-span-2">
            <div className="text-center lg:text-left">
              <div className={`text-xl font-bold ${getAmountColor(claim.amount)}`}>
                {claim.amount}
              </div>
              <div className="text-xs text-wex-gray-500 font-medium">Claim Amount</div>
            </div>
          </div>

          {/* Merchant & Description */}
          <div className="lg:col-span-3">
            <div>
              <div className="font-semibold text-gray-900 truncate" title={claim.merchant}>
                {claim.merchant}
              </div>
              <div className="text-sm text-gray-600 truncate" title={claim.description}>
                {claim.description}
              </div>
              <div className="text-xs text-wex-blue font-medium mt-1">Provider & Service</div>
            </div>
          </div>

          {/* Status Badge with WEX colors */}
          <div className="lg:col-span-2">
            <div className="flex flex-col items-center lg:items-start space-y-2">
              <div className={`px-4 py-2 rounded-full text-sm font-bold border shadow-sm flex items-center space-x-2 ${
                claim.fraudScore <= 30 
                  ? 'bg-gradient-to-r from-emerald-100 to-green-100 text-emerald-800 border-emerald-200' 
                  : claim.fraudScore <= 70 
                  ? 'bg-gradient-to-r from-yellow-100 to-orange-100 text-yellow-800 border-yellow-300' 
                  : 'bg-gradient-to-r from-red-100 to-pink-100 text-red-800 border-red-200'
              }`}>
                <StatusIndicator status={getStatusVariant(claim.fraudScore)} />
                <span>{claim.fraudScore <= 30 ? 'Legitimate' : claim.fraudScore <= 70 ? 'Suspicious' : 'Fraud'}</span>
              </div>
              <div className="w-full max-w-[120px] bg-gray-200 rounded-full h-2">
                <div 
                  className={`h-2 rounded-full transition-all duration-300 ease-out ${
                    claim.fraudScore <= 30 
                      ? 'bg-gradient-to-r from-green-400 to-emerald-500' 
                      : claim.fraudScore <= 70 
                      ? 'bg-gradient-to-r from-yellow-400 to-orange-500' 
                      : 'bg-gradient-to-r from-red-400 to-pink-500'
                  }`}
                  style={{ width: `${claim.fraudScore}%` }}
                />
              </div>
            </div>
          </div>

          {/* Risk Meter */}
          <div className="lg:col-span-1">
            <div className="flex justify-center">
              <FraudScoreMeter score={claim.fraudScore || 0} />
            </div>
          </div>
        </div>

        {/* Mobile View - Additional Info */}
        <div className="lg:hidden mt-4 pt-4 border-t border-wex-blue/20">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div className="bg-gradient-to-r from-wex-blue/5 to-wex-teal/5 p-3 rounded-lg border border-wex-blue/20">
              <span className="text-wex-blue font-medium">Risk Score:</span>
              <span className={`ml-2 font-bold ${getStatusColor(claim.fraudScore)}`}>
                {claim.fraudScore}%
              </span>
            </div>
            <div className="bg-gradient-to-r from-wex-teal/5 to-wex-blue/5 p-3 rounded-lg border border-wex-teal/20">
              <span className="text-wex-teal font-medium">Status:</span>
              <span className={`ml-2 font-bold ${getStatusColor(claim.fraudScore)}`}>
                {claim.fraudScore <= 30 ? 'Legitimate' : claim.fraudScore <= 70 ? 'Suspicious' : 'Fraud'}
              </span>
            </div>
          </div>
        </div>
      </div>
    </motion.div>
  );
};

export default ClaimCard;