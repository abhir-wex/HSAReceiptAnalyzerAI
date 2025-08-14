import React, { useState, useEffect, useRef } from 'react';
import { FaPaperclip, FaPaperPlane } from 'react-icons/fa';
import ReactMarkdown from 'react-markdown';
import './chatUI.css';

export default function AIClaimAssistant() {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [responseCache, setResponseCache] = useState(new Map());
  const messagesEndRef = useRef(null);

  const examplePrompts = [
    "Summarize recent claim patterns for fraud indicators",
    "Show users summary who have submitted similar receipts",
    "Show me claims with unusual spending patterns",
    "Find unusual submission times including late night and weekend claims",
    "Check for claims with round amounts like $100, $50, $250 that appear frequently",
    "Analyze claims for suspicious patterns and unusual behavior"
  ];

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, isLoading]);

  const handleExampleClick = (prompt) => {
    setInput(prompt);
  };

  const clearCache = () => {
    setResponseCache(new Map());
    console.log('Cache cleared');
  };

  const sendMessage = async (messageText = null) => {
    const textToSend = typeof messageText === 'string' ? messageText : input.trim();
    if (!textToSend || isLoading) return;
    
    const userMsg = { sender: 'user', text: textToSend };
    setMessages(prev => [...prev, userMsg]);
    setInput("");
    setIsLoading(true);

    try {
      // Check cache first
      if (responseCache.has(textToSend)) {
        console.log('Using cached response for:', textToSend);
        const cachedResponse = responseCache.get(textToSend);
        const aiMsg = { sender: 'ai', text: cachedResponse };
        setMessages(prev => [...prev, aiMsg]);
        setIsLoading(false);
        return;
      }

      // Call your fraud analysis endpoint
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
      console.log('Cached response for:', textToSend);

      const aiMsg = { sender: 'ai', text: responseText };
      setMessages(prev => [...prev, aiMsg]);
    } catch (error) {
      console.error('Error calling API:', error);
      const errorMsg = { sender: 'ai', text: 'Sorry, I encountered an error processing your request. Please try again.' };
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

  return (
    <div className="chat-container">
      <div className="chat-header">
        <span>FraudLens</span>
        <div className="cache-info">
          <small>Cache: {responseCache.size} items</small>
          {responseCache.size > 0 && (
            <button 
              className="clear-cache-btn" 
              onClick={clearCache}
              title="Clear cache"
            >
              Clear
            </button>
          )}
        </div>
      </div>
      <div className="chat-messages">
        {/* Always show example prompts at the top */}
        <div className="example-prompts">
          <div className="example-prompts-title">Quick Questions:</div>
          {examplePrompts.map((prompt, i) => (
            <button
              key={i}
              className="example-prompt-btn"
              onClick={() => {
                setInput(prompt);
              }}
            >
              {prompt}
            </button>
          ))}
        </div>
        
        {/* Chat messages */}
        {messages.map((msg, i) => (
          <div key={i} className={`chat-bubble ${msg.sender}`}>
            {msg.risk && <div className="risk-badge">Risk: {msg.risk}%</div>}
            {msg.sender === 'ai' ? (
              <ReactMarkdown>
                {typeof msg.text === 'string' ? msg.text : JSON.stringify(msg.text)}
              </ReactMarkdown>
            ) : (
              typeof msg.text === 'string' ? msg.text : JSON.stringify(msg.text)
            )}
          </div>
        ))}
        {isLoading && (
          <div className="typing-indicator">
            <span>Analyzing...</span>
            <div className="typing-dots">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>
      <div className="chat-input">
        <label htmlFor="file-upload">
          <FaPaperclip />
        </label>
        <input id="file-upload" type="file" style={{ display: 'none' }} />
        <input
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Ask about a claim..."
          disabled={isLoading}
        />
        <button onClick={sendMessage} disabled={isLoading}>
          <FaPaperPlane />
        </button>
      </div>
    </div>
  );
}
