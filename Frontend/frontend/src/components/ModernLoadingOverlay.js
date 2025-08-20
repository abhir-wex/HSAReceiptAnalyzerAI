import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';

const ModernLoadingOverlay = ({ 
  isVisible, 
  message = "Processing...", 
  subMessage = "Please wait while we analyze your request",
  progress = null 
}) => {
  return (
    <AnimatePresence>
      {isVisible && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/30 backdrop-blur-sm"
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.9, y: 20 }}
            transition={{ type: "spring", damping: 20, stiffness: 300 }}
            className="glass-wex rounded-3xl p-8 shadow-2xl border border-wex-blue/20 max-w-md w-full mx-4"
          >
            {/* Header with WEX branding */}
            <div className="text-center mb-6">
              <div className="flex items-center justify-center mb-4">
                <div className="w-16 h-16 bg-gradient-to-br from-wex-blue to-wex-teal rounded-2xl flex items-center justify-center shadow-lg">
                  <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
              </div>
              <h3 className="text-xl font-bold text-gray-900 mb-2">{message}</h3>
              <p className="text-gray-600 text-sm">{subMessage}</p>
            </div>

            {/* Advanced Loading Animation with WEX colors */}
            <div className="flex justify-center mb-6">
              <div className="relative">
                {/* Outer Ring with WEX colors */}
                <motion.div
                  animate={{ rotate: 360 }}
                  transition={{ duration: 2, repeat: Infinity, ease: "linear" }}
                  className="w-16 h-16 border-4 border-wex-lightBlue/30 rounded-full"
                />
                
                {/* Inner Ring with WEX gradient */}
                <motion.div
                  animate={{ rotate: -360 }}
                  transition={{ duration: 1.5, repeat: Infinity, ease: "linear" }}
                  className="absolute inset-0 w-16 h-16 rounded-full"
                  style={{
                    border: '4px solid transparent',
                    borderImage: 'linear-gradient(45deg, #4A90E2, #17A2B8, #FFB81C, #C8102E) 1',
                    borderStyle: 'solid'
                  }}
                />
                
                {/* Center Dot with WEX colors */}
                <motion.div
                  animate={{ scale: [1, 1.2, 1] }}
                  transition={{ duration: 1, repeat: Infinity }}
                  className="absolute inset-0 flex items-center justify-center"
                >
                  <div className="w-4 h-4 bg-gradient-to-br from-wex-blue to-wex-teal rounded-full shadow-lg" />
                </motion.div>
              </div>
            </div>

            {/* Progress Bar (if progress is provided) */}
            {progress !== null && (
              <div className="mb-6">
                <div className="flex justify-between items-center mb-2">
                  <span className="text-sm font-medium text-gray-700">Progress</span>
                  <span className="text-sm font-medium text-wex-blue">{Math.round(progress)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <motion.div
                    initial={{ width: 0 }}
                    animate={{ width: `${progress}%` }}
                    transition={{ duration: 0.5, ease: "easeOut" }}
                    className="h-2 bg-gradient-to-r from-wex-blue via-wex-teal to-wex-yellow rounded-full"
                  />
                </div>
              </div>
            )}

            {/* Loading Steps Animation with WEX colors */}
            <div className="space-y-3">
              {[
                { text: "Initializing fraud detection...", delay: 0, color: "wex-blue" },
                { text: "Analyzing receipt data...", delay: 1, color: "wex-teal" },
                { text: "Cross-referencing patterns...", delay: 2, color: "wex-yellow" },
                { text: "Generating risk assessment...", delay: 3, color: "wex-red" }
              ].map((step, index) => (
                <motion.div
                  key={index}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: step.delay, duration: 0.5 }}
                  className="flex items-center space-x-3"
                >
                  <motion.div
                    animate={{ rotate: 360 }}
                    transition={{ 
                      delay: step.delay,
                      duration: 1, 
                      repeat: Infinity, 
                      ease: "linear" 
                    }}
                    className={`w-4 h-4 border-2 border-${step.color}/30 rounded-full`}
                    style={{
                      borderTopColor: step.color === 'wex-blue' ? '#4A90E2' : 
                                     step.color === 'wex-teal' ? '#17A2B8' :
                                     step.color === 'wex-yellow' ? '#FFB81C' : '#C8102E'
                    }}
                  />
                  <span className="text-sm text-gray-600">{step.text}</span>
                </motion.div>
              ))}
            </div>

            {/* Decorative Elements with WEX colors */}
            <div className="absolute top-4 right-4 w-2 h-2 bg-wex-blue rounded-full animate-ping" />
            <div className="absolute bottom-4 left-4 w-2 h-2 bg-wex-teal rounded-full animate-pulse" />
            <div className="absolute top-6 left-6 w-1 h-1 bg-wex-yellow rounded-full animate-pulse" />
            <div className="absolute bottom-6 right-6 w-1 h-1 bg-wex-red rounded-full animate-ping" />
            
            {/* Background Particles with WEX colors */}
            <div className="absolute inset-0 overflow-hidden rounded-3xl pointer-events-none">
              {[...Array(8)].map((_, i) => (
                <motion.div
                  key={i}
                  initial={{ 
                    x: Math.random() * 400, 
                    y: Math.random() * 400,
                    opacity: 0 
                  }}
                  animate={{ 
                    x: Math.random() * 400, 
                    y: Math.random() * 400,
                    opacity: [0, 0.4, 0]
                  }}
                  transition={{ 
                    duration: 3 + Math.random() * 2, 
                    repeat: Infinity,
                    delay: Math.random() * 2
                  }}
                  className={`absolute w-1 h-1 rounded-full ${
                    i % 4 === 0 ? 'bg-wex-blue' :
                    i % 4 === 1 ? 'bg-wex-teal' :
                    i % 4 === 2 ? 'bg-wex-yellow' : 'bg-wex-red'
                  }`}
                />
              ))}
            </div>

            {/* WEX Footer */}
            <div className="mt-6 text-center">
              <div className="text-xs text-wex-blue font-medium bg-gradient-to-r from-wex-blue/10 to-wex-teal/10 px-3 py-1 rounded-full border border-wex-blue/20">
                Powered by WEX AI Technology
              </div>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
};

export default ModernLoadingOverlay;