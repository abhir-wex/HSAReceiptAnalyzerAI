import React from 'react';
import { motion } from 'framer-motion';

export const Card = ({ children, className = '', ...props }) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className={`glass-wex rounded-2xl shadow-xl border border-wex-blue/20 transition-all duration-300 hover:shadow-2xl ${className}`}
      {...props}
    >
      {children}
    </motion.div>
  );
};

export const Button = ({ 
  children, 
  variant = 'primary', 
  size = 'md', 
  className = '', 
  loading = false,
  disabled = false,
  ...props 
}) => {
  const baseClasses = 'inline-flex items-center justify-center font-semibold rounded-xl transition-all duration-300 focus:outline-none focus:ring-4 disabled:opacity-50 disabled:cursor-not-allowed';
  
  const variants = {
    primary: 'bg-gradient-to-r from-wex-blue to-wex-teal text-white shadow-lg hover:shadow-xl transform hover:scale-105 focus:ring-wex-blue/30',
    secondary: 'bg-white text-gray-600 border border-wex-blue/20 hover:bg-white/90 shadow-lg hover:shadow-xl transform hover:scale-105 focus:ring-wex-blue/30',
    success: 'bg-gradient-to-r from-emerald-500 to-green-600 text-white shadow-lg hover:shadow-xl transform hover:scale-105 focus:ring-green-300',
    danger: 'bg-gradient-to-r from-wex-red to-red-600 text-white shadow-lg hover:shadow-xl transform hover:scale-105 focus:ring-red-300',
  };
  
  const sizes = {
    sm: 'px-4 py-2 text-sm',
    md: 'px-6 py-3 text-base',
    lg: 'px-8 py-4 text-lg',
  };
  
  return (
    <motion.button
      whileHover={{ scale: disabled || loading ? 1 : 1.05 }}
      whileTap={{ scale: disabled || loading ? 1 : 0.95 }}
      className={`${baseClasses} ${variants[variant]} ${sizes[size]} ${className}`}
      disabled={disabled || loading}
      {...props}
    >
      {loading && (
        <svg className="animate-spin -ml-1 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      )}
      {children}
    </motion.button>
  );
};

export const Badge = ({ children, variant = 'primary', className = '' }) => {
  const variants = {
    legitimate: 'bg-gradient-to-r from-emerald-100 to-green-100 text-emerald-800 border-emerald-200',
    suspicious: 'bg-gradient-to-r from-yellow-100 to-orange-100 text-yellow-800 border-yellow-200',
    fraud: 'bg-gradient-to-r from-red-100 to-pink-100 text-red-800 border-red-200',
    primary: 'bg-gradient-to-r from-wex-blue/10 to-wex-teal/10 text-wex-blue border-wex-blue/20',
    secondary: 'bg-gradient-to-r from-gray-100 to-gray-200 text-gray-800 border-gray-300',
  };
  
  return (
    <span className={`px-3 py-1 rounded-full text-sm font-bold border shadow-sm ${variants[variant]} ${className}`}>
      {children}
    </span>
  );
};

export const Input = ({ 
  label, 
  error, 
  className = '', 
  required = false,
  type = 'text',
  ...props 
}) => {
  return (
    <div className="form-group">
      {label && (
        <label className="form-label">
          {label}
          {required && <span className="text-wex-red ml-1">*</span>}
        </label>
      )}
      <input
        type={type}
        className={`form-input ${error ? 'border-wex-red focus:border-wex-red focus:ring-wex-red/20' : ''} ${className}`}
        {...props}
      />
      {error && (
        <p className="mt-2 text-sm text-wex-red">{error}</p>
      )}
    </div>
  );
};

export const Select = ({ 
  label, 
  options = [], 
  error, 
  className = '', 
  required = false,
  ...props 
}) => {
  return (
    <div className="form-group">
      {label && (
        <label className="form-label">
          {label}
          {required && <span className="text-wex-red ml-1">*</span>}
        </label>
      )}
      <div className="relative">
        <select
          className={`form-select ${error ? 'border-wex-red focus:border-wex-red focus:ring-wex-red/20' : ''} ${className}`}
          {...props}
        >
          {options.map((option, index) => (
            <option key={index} value={option.value} className="bg-white text-gray-900">
              {option.label}
            </option>
          ))}
        </select>
        <div className="absolute inset-y-0 right-0 flex items-center px-2 pointer-events-none">
          <svg className="w-5 h-5 text-wex-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </div>
      {error && (
        <p className="mt-2 text-sm text-wex-red">{error}</p>
      )}
    </div>
  );
};

export const Textarea = ({ 
  label, 
  error, 
  className = '', 
  required = false,
  rows = 4,
  ...props 
}) => {
  return (
    <div className="form-group">
      {label && (
        <label className="form-label">
          {label}
          {required && <span className="text-wex-red ml-1">*</span>}
        </label>
      )}
      <textarea
        rows={rows}
        className={`form-textarea ${error ? 'border-wex-red focus:border-wex-red focus:ring-wex-red/20' : ''} ${className}`}
        {...props}
      />
      {error && (
        <p className="mt-2 text-sm text-wex-red">{error}</p>
      )}
    </div>
  );
};

export const Modal = ({ isOpen, onClose, title, children, size = 'md' }) => {
  if (!isOpen) return null;
  
  const sizes = {
    sm: 'max-w-md',
    md: 'max-w-2xl',
    lg: 'max-w-4xl',
    xl: 'max-w-6xl',
  };
  
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="modal-backdrop"
      onClick={onClose}
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className={`modal-content ${sizes[size]}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="p-6 border-b border-wex-blue/20">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-bold text-gray-900">{title}</h2>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-wex-blue transition-colors"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
        <div className="p-6">
          {children}
        </div>
      </motion.div>
    </motion.div>
  );
};

export const LoadingSpinner = ({ size = 'md', className = '' }) => {
  const sizes = {
    sm: 'w-4 h-4',
    md: 'w-8 h-8',
    lg: 'w-12 h-12',
  };
  
  return (
    <div className={`inline-flex items-center justify-center ${className}`}>
      <svg 
        className={`animate-spin ${sizes[size]} text-current`} 
        xmlns="http://www.w3.org/2000/svg" 
        fill="none" 
        viewBox="0 0 24 24"
      >
        <circle 
          className="opacity-25" 
          cx="12" 
          cy="12" 
          r="10" 
          stroke="currentColor" 
          strokeWidth="4"
        />
        <path 
          className="opacity-75" 
          fill="currentColor" 
          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
        />
      </svg>
    </div>
  );
};

export const ProgressBar = ({ value = 0, max = 100, variant = 'primary', className = '' }) => {
  const percentage = Math.min(Math.max((value / max) * 100, 0), 100);
  
  const getVariant = () => {
    if (variant === 'auto') {
      if (percentage <= 30) return 'legitimate';
      if (percentage <= 70) return 'suspicious';
      return 'fraud';
    }
    return variant;
  };
  
  return (
    <div className={`progress-bar ${className}`}>
      <div 
        className={`progress-fill ${getVariant()}`}
        style={{ width: `${percentage}%` }}
      />
    </div>
  );
};

export const StatusIndicator = ({ status, className = '' }) => {
  const statusClasses = {
    legitimate: 'status-legitimate',
    suspicious: 'status-suspicious',
    fraud: 'status-fraud',
  };
  
  return (
    <div className={`status-indicator ${statusClasses[status] || statusClasses.legitimate} ${className}`} />
  );
};