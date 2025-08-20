import React, { useState, useEffect, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import ReactMarkdown from 'react-markdown';

const ModernChatAssistant = () => {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [responseCache, setResponseCache] = useState(new Map());
  const messagesEndRef = useRef(null);

  const examplePrompts = [
    {
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      ),
      title: "Fraud Patterns",
      prompt: "Summarize recent claim patterns for fraud indicators",
      color: "from-wex-blue/10 to-wex-teal/10 border-wex-blue/20"
    },
    {
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
        </svg>
      ),
      title: "Shared Receipts",
      prompt: "Show users summary who have submitted similar receipts",
      color: "from-wex-teal/10 to-wex-lightBlue/10 border-wex-teal/20"
    },
    {
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      title: "Spending Patterns",
      prompt: "Show me claims with unusual spending patterns",
      color: "from-wex-yellow/10 to-wex-blue/10 border-wex-yellow/20"
    },
    {
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      title: "Timing Analysis",
      prompt: "Find unusual submission times including late night and weekend claims",
      color: "from-wex-red/10 to-wex-yellow/10 border-wex-red/20"
    },
    {
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      title: "Round Amounts",
      prompt: "Check for claims with round amounts like $100, $50, $250 that appear frequently",
      color: "from-wex-lightBlue/10 to-wex-blue/10 border-wex-lightBlue/20"
    },
    {
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
      ),
      title: "Suspicious Behavior",
      prompt: "Analyze claims for suspicious patterns and unusual behavior",
      color: "from-wex-blue/10 to-wex-red/10 border-wex-blue/20"
    }
  ];

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, isLoading]);

  const sendMessage = async (messageText = null) => {
    const textToSend = typeof messageText === 'string' ? messageText : input.trim();
    if (!textToSend || isLoading) return;
    
    const userMsg = { 
      id: Date.now(),
      sender: 'user', 
      text: textToSend,
      timestamp: new Date()
    };
    
    setMessages(prev => [...prev, userMsg]);
    setInput("");
    setIsLoading(true);

    try {
      // Check cache first
      if (responseCache.has(textToSend)) {
        console.log('Using cached response for:', textToSend);
        const cachedResponse = responseCache.get(textToSend);
        const aiMsg = { 
          id: Date.now() + 1,
          sender: 'ai', 
          text: cachedResponse,
          timestamp: new Date()
        };
        setMessages(prev => [...prev, aiMsg]);
        setIsLoading(false);
        return;
      }

      // Call fraud analysis endpoint
      const res = await fetch("/Analyze/adminAnalyze", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ Prompt: textToSend })
      });
      
      const data = await res.json();
      
      // Handle different response formats
      let responseText = '';
      if (typeof data === 'string') {
        responseText = data;
      } else if (data.summary) {
        responseText = typeof data.summary === 'string' ? data.summary : JSON.stringify(data.summary);
      } else if (data.response) {
        responseText = typeof data.response === 'string' ? data.response : JSON.stringify(data.response);
      } else if (data.results) {
        responseText = typeof data.results === 'string' ? data.results : JSON.stringify(data.results);
      } else {
        responseText = JSON.stringify(data);
      }

      // Cache the response
      setResponseCache(prev => new Map(prev.set(textToSend, responseText)));

      const aiMsg = { 
        id: Date.now() + 1,
        sender: 'ai', 
        text: responseText,
        timestamp: new Date()
      };
      setMessages(prev => [...prev, aiMsg]);
    } catch (error) {
      console.error('Error calling API:', error);
      const errorMsg = { 
        id: Date.now() + 1,
        sender: 'ai', 
        text: 'Sorry, I encountered an error processing your request. Please try again.',
        timestamp: new Date()
      };
      setMessages(prev => [...prev, errorMsg]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  const clearChat = () => {
    setMessages([]);
    setResponseCache(new Map());
  };

  return (
    <div className="relative">
      <div className="glass-wex h-[600px] flex flex-col overflow-hidden rounded-3xl shadow-2xl border border-wex-blue/20">
        {/* Header with WEX branding */}
        <div className="bg-gradient-to-r from-wex-blue via-wex-teal to-wex-blue text-white p-4 flex items-center justify-between rounded-t-3xl">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 bg-white/20 rounded-full flex items-center justify-center backdrop-blur-sm">
              <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
            </div>
            <div>
              <h3 className="font-bold text-lg">FraudLens AI Assistant</h3>
              <p className="text-wex-lightBlue text-sm">Ask me about fraud patterns & analysis</p>
            </div>
          </div>
          
          <div className="flex items-center space-x-2">
            {responseCache.size > 0 && (
              <button 
                onClick={clearChat}
                className="p-2 bg-white/20 rounded-lg hover:bg-white/30 transition-colors text-sm backdrop-blur-sm"
                title="Clear chat"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            )}
            <div className="text-xs bg-white/20 px-2 py-1 rounded-full backdrop-blur-sm">
              {responseCache.size} cached
            </div>
          </div>
        </div>

        {/* Messages Area */}
        <div className="flex-1 overflow-y-auto p-4 bg-gradient-to-b from-white/50 to-wex-lightBlue/10 space-y-4">
          {/* Welcome Message */}
          {messages.length === 0 && (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="text-center py-8"
            >
              <div className="flex justify-center mb-4">
                <div className="w-16 h-16 bg-gradient-to-br from-wex-blue to-wex-teal rounded-2xl flex items-center justify-center shadow-lg">
                  <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                  </svg>
                </div>
              </div>
              <h4 className="text-xl font-bold text-gray-800 mb-2">Welcome to FraudLens AI</h4>
              <p className="text-gray-600 mb-6">I can help you analyze fraud patterns, detect suspicious activities, and provide insights about claims data.</p>
              
              {/* Quick Action Buttons with improved layout */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 max-w-4xl mx-auto">
                {examplePrompts.map((prompt, index) => (
                  <motion.button
                    key={index}
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: index * 0.1 }}
                    whileHover={{ scale: 1.02 }}
                    whileTap={{ scale: 0.98 }}
                    onClick={() => setInput(prompt.prompt)}
                    className={`chat-prompt-button bg-gradient-to-r ${prompt.color} rounded-xl shadow-sm hover:shadow-md border`}
                  >
                    <div className="chat-prompt-content">
                      <span className="chat-prompt-icon text-wex-blue">{prompt.icon}</span>
                      <div className="chat-prompt-text">
                        <div className="chat-prompt-title">
                          {prompt.title}
                        </div>
                        <div className="chat-prompt-description">
                          {prompt.prompt}
                        </div>
                      </div>
                    </div>
                  </motion.button>
                ))}
              </div>
            </motion.div>
          )}

          {/* Chat Messages */}
          <AnimatePresence>
            {messages.map((message) => (
              <motion.div
                key={message.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -20 }}
                className={`flex ${message.sender === 'user' ? 'justify-end' : 'justify-start'}`}
              >
                <div className={`max-w-[80%] ${message.sender === 'user' ? 'order-2' : 'order-1'}`}>
                  <div className={`p-4 rounded-2xl shadow-sm border ${
                    message.sender === 'user' 
                      ? 'bg-gradient-to-r from-wex-blue to-wex-teal text-white ml-4 border-wex-blue/20' 
                      : 'bg-white/90 border-wex-blue/20 mr-4 backdrop-blur-sm'
                  }`}>
                    {message.sender === 'ai' ? (
                      <div className="prose prose-sm max-w-none">
                        <ReactMarkdown
                          components={{
                            h1: ({children}) => <h1 className="text-lg font-bold text-wex-blue mb-2">{children}</h1>,
                            h2: ({children}) => <h2 className="text-base font-bold text-wex-teal mb-2">{children}</h2>,
                            h3: ({children}) => <h3 className="text-sm font-bold text-wex-blue mb-1">{children}</h3>,
                            p: ({children}) => <p className="text-gray-700 mb-2 last:mb-0">{children}</p>,
                            ul: ({children}) => <ul className="list-disc list-inside text-gray-700 mb-2">{children}</ul>,
                            li: ({children}) => <li className="mb-1">{children}</li>,
                            strong: ({children}) => <strong className="font-bold text-wex-blue">{children}</strong>,
                            code: ({children}) => <code className="bg-wex-lightBlue/20 px-1 py-0.5 rounded text-sm font-mono text-wex-blue">{children}</code>,
                          }}
                        >
                          {message.text}
                        </ReactMarkdown>
                      </div>
                    ) : (
                      <div className="text-white">{message.text}</div>
                    )}
                  </div>
                  <div className={`text-xs text-gray-500 mt-1 ${
                    message.sender === 'user' ? 'text-right mr-4' : 'text-left ml-4'
                  }`}>
                    {message.timestamp.toLocaleTimeString()}
                  </div>
                </div>
                
                {/* Avatar with WEX colors */}
                <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm ${
                  message.sender === 'user' 
                    ? 'bg-gradient-to-br from-wex-blue to-wex-teal text-white order-1' 
                    : 'bg-gradient-to-br from-wex-lightBlue to-white text-wex-blue border border-wex-blue/20 order-2'
                }`}>
                  {message.sender === 'user' ? (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                    </svg>
                  ) : (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                  )}
                </div>
              </motion.div>
            ))}
          </AnimatePresence>

          {/* Typing Indicator with WEX colors */}
          {isLoading && (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="flex justify-start"
            >
              <div className="flex items-center space-x-3">
                <div className="w-8 h-8 bg-gradient-to-br from-wex-lightBlue to-white rounded-full flex items-center justify-center text-sm border border-wex-blue/20">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                  </svg>
                </div>
                <div className="bg-white/90 border border-wex-blue/20 rounded-2xl p-4 mr-4 backdrop-blur-sm">
                  <div className="flex items-center space-x-2">
                    <div className="flex space-x-1">
                      {[0, 1, 2].map((i) => (
                        <motion.div
                          key={i}
                          animate={{ scale: [1, 1.2, 1] }}
                          transition={{ 
                            duration: 0.6, 
                            repeat: Infinity, 
                            delay: i * 0.2 
                          }}
                          className={`w-2 h-2 rounded-full ${
                            i === 0 ? 'bg-wex-blue' : i === 1 ? 'bg-wex-teal' : 'bg-wex-yellow'
                          }`}
                        />
                      ))}
                    </div>
                    <span className="text-wex-blue text-sm font-medium">Analyzing data...</span>
                  </div>
                </div>
              </div>
            </motion.div>
          )}

          <div ref={messagesEndRef} />
        </div>

        {/* Input Area with WEX styling */}
        <div className="p-4 bg-white/80 border-t border-wex-blue/20 rounded-b-3xl backdrop-blur-sm">
          <div className="flex items-center space-x-3">
            <div className="flex-1 relative">
              <input
                type="text"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder="Ask about fraud patterns, suspicious activities..."
                className="w-full px-4 py-3 border border-wex-blue/30 rounded-xl focus:ring-2 focus:ring-wex-blue/50 focus:border-wex-blue transition-all duration-200 bg-white/90 focus:bg-white backdrop-blur-sm"
                disabled={isLoading}
              />
            </div>
            
            <button
              onClick={() => sendMessage()}
              disabled={!input.trim() || isLoading}
              className={`px-6 py-3 rounded-xl font-semibold transition-all duration-300 ${
                !input.trim() || isLoading
                  ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                  : 'bg-gradient-to-r from-wex-blue to-wex-teal text-white hover:shadow-lg transform hover:scale-105'
              }`}
            >
              {isLoading ? (
                <div className="flex items-center space-x-2">
                  <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                </div>
              ) : (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                </svg>
              )}
            </button>
          </div>
          
          {input && (
            <div className="mt-2 text-xs text-wex-blue">
              Press Enter to send, Shift+Enter for new line
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ModernChatAssistant;