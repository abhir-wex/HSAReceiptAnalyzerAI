import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Input, Textarea } from './UIComponents';

const ModernClaimForm = ({ onSubmit, onCancel, isSubmitting = false }) => {
  const [formData, setFormData] = useState({
    userId: '',
    date: '',
    amount: '',
    merchant: '',
    description: '',
    customPrompt: ''
  });
  
  const [dragActive, setDragActive] = useState(false);
  const [selectedFile, setSelectedFile] = useState(null);
  const [errors, setErrors] = useState({});

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    
    // Clear error when user starts typing
    if (errors[name]) {
      setErrors(prev => ({
        ...prev,
        [name]: ''
      }));
    }
  };

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
      handleFileSelect(e.dataTransfer.files[0]);
    }
  };

  const handleFileSelect = (file) => {
    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/png', 'application/pdf'];
    if (!allowedTypes.includes(file.type)) {
      setErrors(prev => ({
        ...prev,
        file: 'Please select a valid file (JPG, PNG, or PDF)'
      }));
      return;
    }

    // Validate file size (5MB limit)
    if (file.size > 5 * 1024 * 1024) {
      setErrors(prev => ({
        ...prev,
        file: 'File size must be less than 5MB'
      }));
      return;
    }

    setSelectedFile(file);
    setErrors(prev => ({
      ...prev,
      file: ''
    }));
  };

  const validateForm = () => {
    const newErrors = {};
    
    if (!formData.userId.trim()) newErrors.userId = 'User ID is required';
    if (!formData.date) newErrors.date = 'Service date is required';
    if (!formData.amount || parseFloat(formData.amount) <= 0) newErrors.amount = 'Valid amount is required';
    if (!formData.merchant.trim()) newErrors.merchant = 'Merchant name is required';
    if (!formData.description.trim()) newErrors.description = 'Description is required';
    if (!selectedFile) newErrors.file = 'Receipt file is required';
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    // Create FormData object
    const submitData = new FormData();
    Object.keys(formData).forEach(key => {
      submitData.append(key, formData[key]);
    });
    
    if (selectedFile) {
      submitData.append('image', selectedFile);
    }

    onSubmit(submitData);
  };

  const formatFileSize = (bytes) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="w-full max-w-4xl mx-auto"
    >
      <div className="glass-wex p-8 rounded-3xl shadow-2xl border border-wex-blue/20">
        {/* Header with WEX colors */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-wex-blue to-wex-teal rounded-2xl mb-4 shadow-lg">
            <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <h2 className="text-3xl font-bold bg-gradient-to-r from-wex-blue to-wex-teal bg-clip-text text-transparent mb-2">Submit New Claim</h2>
          <p className="text-gray-600 text-lg">Fill out the details below for AI-powered fraud analysis</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Personal Information */}
          <div className="bg-gradient-to-r from-wex-blue/5 to-wex-teal/5 rounded-xl p-6 border border-wex-blue/20">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
              <span className="w-6 h-6 bg-gradient-to-r from-wex-blue to-wex-teal text-white rounded-full flex items-center justify-center text-sm mr-3">1</span>
              Personal Information
            </h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <Input
                label="User ID"
                name="userId"
                value={formData.userId}
                onChange={handleInputChange}
                placeholder="Enter your User ID"
                required
                error={errors.userId}
              />
              
              <Input
                label="Service Date"
                name="date"
                type="date"
                value={formData.date}
                onChange={handleInputChange}
                required
                error={errors.date}
              />
            </div>
          </div>

          {/* Claim Details */}
          <div className="bg-gradient-to-r from-wex-teal/5 to-wex-lightBlue/5 rounded-xl p-6 border border-wex-teal/20">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
              <span className="w-6 h-6 bg-gradient-to-r from-wex-teal to-wex-lightBlue text-white rounded-full flex items-center justify-center text-sm mr-3">2</span>
              Claim Details
            </h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <Input
                label="Amount"
                name="amount"
                type="number"
                step="0.01"
                min="0"
                value={formData.amount}
                onChange={handleInputChange}
                placeholder="0.00"
                required
                error={errors.amount}
              />
              
              <Input
                label="Merchant/Provider"
                name="merchant"
                value={formData.merchant}
                onChange={handleInputChange}
                placeholder="Healthcare provider or merchant name"
                required
                error={errors.merchant}
              />
              
              <div className="md:col-span-2">
                <Input
                  label="Service Description"
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  placeholder="Describe the service or treatment received"
                  required
                  error={errors.description}
                />
              </div>
            </div>
          </div>

          {/* Receipt Upload */}
          <div className="bg-gradient-to-r from-wex-yellow/5 to-wex-blue/5 rounded-xl p-6 border border-wex-yellow/20">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
              <span className="w-6 h-6 bg-gradient-to-r from-wex-yellow to-wex-blue text-white rounded-full flex items-center justify-center text-sm mr-3">3</span>
              Receipt Upload
            </h3>
            
            <div
              className={`border-2 border-dashed rounded-xl p-8 text-center transition-all duration-300 ${
                dragActive 
                  ? 'border-wex-blue bg-gradient-to-r from-wex-blue/10 to-wex-teal/10' 
                  : errors.file 
                  ? 'border-wex-red bg-wex-red/5' 
                  : 'border-wex-blue/30 bg-white/50 hover:border-wex-blue hover:bg-gradient-to-r hover:from-wex-blue/5 hover:to-wex-teal/5'
              }`}
              onDragEnter={handleDrag}
              onDragLeave={handleDrag}
              onDragOver={handleDrag}
              onDrop={handleDrop}
            >
              <input
                type="file"
                accept=".pdf,.jpg,.jpeg,.png"
                onChange={(e) => handleFileSelect(e.target.files[0])}
                className="hidden"
                id="receipt-upload"
              />
              
              {selectedFile ? (
                <div className="space-y-4">
                  <div className="flex items-center justify-center space-x-3">
                    <div className="w-12 h-12 bg-gradient-to-r from-wex-blue to-wex-teal rounded-xl flex items-center justify-center">
                      {selectedFile.type === 'application/pdf' ? (
                        <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                        </svg>
                      ) : (
                        <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                      )}
                    </div>
                    <div className="text-left">
                      <div className="font-semibold text-gray-900">{selectedFile.name}</div>
                      <div className="text-sm text-wex-blue">{formatFileSize(selectedFile.size)}</div>
                    </div>
                  </div>
                  
                  <div className="flex justify-center space-x-3">
                    <label htmlFor="receipt-upload">
                      <div className="btn-wex px-4 py-2 text-sm cursor-pointer">
                        Change File
                      </div>
                    </label>
                    <button 
                      type="button"
                      onClick={() => setSelectedFile(null)}
                      className="px-4 py-2 bg-gradient-to-r from-wex-red to-red-600 text-white rounded-lg hover:shadow-lg transition-all duration-300 text-sm"
                    >
                      Remove
                    </button>
                  </div>
                </div>
              ) : (
                <div className="space-y-4">
                  <div className="flex justify-center">
                    <div className="w-16 h-16 bg-gradient-to-r from-wex-blue to-wex-teal rounded-2xl flex items-center justify-center">
                      <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                      </svg>
                    </div>
                  </div>
                  
                  <div className="text-center">
                    <p className="text-lg font-semibold text-gray-900 mb-2">
                      Drop your receipt here, or{' '}
                      <label htmlFor="receipt-upload" className="text-wex-blue hover:text-wex-teal cursor-pointer underline font-bold">
                        browse files
                      </label>
                    </p>
                    <p className="text-gray-600">
                      Supports PDF, JPG, PNG files up to 5MB
                    </p>
                  </div>
                </div>
              )}
            </div>
            
            {errors.file && (
              <p className="mt-2 text-sm text-wex-red">{errors.file}</p>
            )}
          </div>

          {/* Additional Notes */}
          <div className="bg-gradient-to-r from-wex-red/5 to-wex-yellow/5 rounded-xl p-6 border border-wex-red/20">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
              <span className="w-6 h-6 bg-gradient-to-r from-wex-red to-wex-yellow text-white rounded-full flex items-center justify-center text-sm mr-3">4</span>
              Additional Notes (Optional)
            </h3>
            
            <Textarea
              label="Special Instructions or Context"
              name="customPrompt"
              value={formData.customPrompt}
              onChange={handleInputChange}
              placeholder="Any additional context, special circumstances, or instructions for the AI analysis..."
              rows={4}
            />
          </div>

          {/* Action Buttons */}
          <div className="flex flex-col sm:flex-row justify-center space-y-3 sm:space-y-0 sm:space-x-4 pt-6">
            <button
              type="button"
              onClick={onCancel}
              disabled={isSubmitting}
              className="px-8 py-4 bg-gradient-to-r from-wex-gray-500 to-wex-gray-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl transform hover:scale-105 transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed order-2 sm:order-1"
            >
              <svg className="w-5 h-5 mr-2 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
              Cancel
            </button>
            
            <button
              type="submit"
              disabled={isSubmitting}
              className={`px-8 py-4 rounded-xl font-semibold shadow-lg transition-all duration-300 order-1 sm:order-2 ${
                isSubmitting
                  ? 'bg-gradient-to-r from-wex-blue/50 to-wex-teal/50 cursor-not-allowed'
                  : 'bg-gradient-to-r from-wex-blue to-wex-teal hover:shadow-xl transform hover:scale-105'
              } text-white`}
            >
              <svg className="w-5 h-5 mr-2 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              {isSubmitting ? 'Analyzing...' : 'Analyze & Submit Claim'}
            </button>
          </div>
        </form>
      </div>
    </motion.div>
  );
};

export default ModernClaimForm;