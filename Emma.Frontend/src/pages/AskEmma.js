import React, { useState, useRef, useEffect } from 'react';
import { Send, Mic, MicOff, Sparkles, User, Users, TrendingUp, AlertCircle, Phone } from 'lucide-react';
import aiService from '../services/aiService';

const AskEmma = () => {
  const [messages, setMessages] = useState([
    {
      id: 1,
      sender: 'assistant',
      text: "Hi! I'm EMMA, your AI real estate assistant. I can help you with insights about your contacts, market trends, follow-up priorities, and strategic recommendations. What would you like to know?",
      timestamp: new Date().toLocaleTimeString(),
      insights: [
        { icon: 'Sparkles', text: "AI-powered insights ready" },
        { icon: 'Users', text: "Contact data available" },
        { icon: 'TrendingUp', text: "Market insights ready" }
      ]
    }
  ]);
  const [inputValue, setInputValue] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isListening, setIsListening] = useState(false);
  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // Icon mapping for insights
  const insightIcons = {
    'Sparkles': Sparkles,
    'User': User,
    'Users': Users,
    'TrendingUp': TrendingUp,
    'AlertCircle': AlertCircle,
    'Phone': Phone
  };

  const handleSendMessage = async () => {
    if (!inputValue.trim() || isLoading) return;

    const userMessage = {
      id: Date.now(),
      sender: 'user',
      text: inputValue.trim(),
      timestamp: new Date().toLocaleTimeString()
    };

    setMessages(prev => [...prev, userMessage]);
    setInputValue('');
    setIsLoading(true);

    try {
      // Get AI response using the real AI service
      const aiResponse = await aiService.askEmma(userMessage.text, messages);
      
      const assistantMessage = {
        id: Date.now() + 1,
        sender: 'assistant',
        text: aiResponse.content,
        timestamp: new Date().toLocaleTimeString(),
        insights: aiResponse.insights.map(insight => ({
          ...insight,
          icon: insightIcons[insight.icon] || Sparkles
        }))
      };

      setMessages(prev => [...prev, assistantMessage]);
    } catch (error) {
      console.error('Error getting AI response:', error);
      
      // Fallback message
      const errorMessage = {
        id: Date.now() + 1,
        sender: 'assistant',
        text: "I'm having trouble processing your request right now. Please try again, or ask about specific contacts like Chris Gabriel, Emily Johnson, or current market trends.",
        timestamp: new Date().toLocaleTimeString(),
        insights: [
          { icon: AlertCircle, text: "Service temporarily unavailable", color: "text-orange-600" }
        ]
      };
      
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const handleSampleQuery = (query) => {
    setInputValue(query);
  };

  const toggleListening = () => {
    setIsListening(!isListening);
    // In a real implementation, this would integrate with speech recognition
  };

  const handleTestConnection = async () => {
    try {
      setIsLoading(true);
      const testResponse = await aiService.testConnection();
      console.log(testResponse); // Fix the lint error by logging the response
    } catch (error) {
      console.error('Error testing connection:', error);
    } finally {
      setIsLoading(false);
    }
  };

  // Sample queries for quick demo
  const sampleQueries = [
    "What's the status of Chris Gabriel's property decision?",
    "Who needs follow-up this week?",
    "What are the main concerns from recent property inspections?",
    "Show me contacts with competing offers",
    "What's the next best action for my prospects?",
    "Analyze recent interaction patterns"
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto p-6">
        {/* Header */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 mb-6">
          <div className="flex items-center space-x-3 mb-4">
            <div className="bg-gradient-to-r from-purple-600 to-blue-600 p-3 rounded-lg">
              <Sparkles className="h-6 w-6 text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Ask EMMA Anything</h1>
              <p className="text-gray-600">Your AI-powered real estate assistant</p>
            </div>
          </div>
          
          {/* Sample Queries */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            {sampleQueries.map((query, index) => (
              <button
                key={index}
                onClick={() => handleSampleQuery(query)}
                className="text-left p-3 bg-gray-50 hover:bg-blue-50 border border-gray-200 hover:border-blue-300 rounded-lg transition-colors text-sm"
              >
                <span className="text-blue-600 font-medium">"{query}"</span>
              </button>
            ))}
          </div>
        </div>

        {/* Chat Interface */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 flex flex-col" style={{ height: '600px' }}>
          {/* Messages */}
          <div className="flex-1 overflow-y-auto p-6 space-y-4">
            {messages.map((message, index) => (
              <div key={index} className={`flex ${message.sender === 'user' ? 'justify-end' : 'justify-start'}`}>
                <div className={`flex space-x-3 max-w-3xl ${message.sender === 'user' ? 'flex-row-reverse space-x-reverse' : ''}`}>
                  <div className={`flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center ${
                    message.sender === 'user' 
                      ? 'bg-blue-600' 
                      : 'bg-gradient-to-r from-purple-600 to-blue-600'
                  }`}>
                    {message.sender === 'user' ? (
                      <User className="h-4 w-4 text-white" />
                    ) : (
                      <Sparkles className="h-4 w-4 text-white" />
                    )}
                  </div>
                  <div className="flex-1">
                    <div className={`inline-block p-4 rounded-lg ${
                      message.sender === 'user'
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-100 text-gray-900'
                    }`}>
                      <div className="whitespace-pre-wrap">{message.text}</div>
                      {message.timestamp && (
                        <div className={`text-xs mt-2 ${
                          message.sender === 'user' ? 'text-blue-100' : 'text-gray-500'
                        }`}>
                          {message.timestamp}
                        </div>
                      )}
                    </div>
                    {message.insights && message.insights.length > 0 && (
                      <div className="mt-3 flex flex-wrap gap-2">
                        {message.insights.map((insight, idx) => {
                          const IconComponent = insightIcons[insight.icon] || AlertCircle;
                          return (
                            <div key={idx} className="flex items-center space-x-2 bg-blue-50 text-blue-700 px-3 py-1 rounded-full text-sm">
                              <IconComponent className="h-4 w-4" />
                              <span>{insight.text}</span>
                            </div>
                          );
                        })}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ))}
            {isLoading && (
              <div className="flex justify-start">
                <div className="flex space-x-3 max-w-3xl">
                  <div className="flex-shrink-0 w-8 h-8 rounded-full bg-gradient-to-r from-purple-600 to-blue-600 flex items-center justify-center">
                    <Sparkles className="h-4 w-4 text-white" />
                  </div>
                  <div className="flex-1">
                    <div className="inline-block p-4 rounded-lg bg-gray-100">
                      <div className="flex space-x-1">
                        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></div>
                        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{animationDelay: '0.1s'}}></div>
                        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{animationDelay: '0.2s'}}></div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Input Area */}
          <div className="border-t border-gray-200 p-4">
            <div className="flex space-x-3">
              <div className="flex-1 relative">
                <textarea
                  value={inputValue}
                  onChange={(e) => setInputValue(e.target.value)}
                  onKeyPress={handleKeyPress}
                  placeholder="Ask EMMA anything about your contacts, interactions, or next best actions..."
                  className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                  rows="2"
                  disabled={isLoading}
                />
              </div>
              <div className="flex flex-col space-y-2">
                <button
                  onClick={toggleListening}
                  className={`p-3 rounded-lg border transition-colors ${
                    isListening
                      ? 'bg-red-600 text-white border-red-600'
                      : 'bg-gray-100 text-gray-600 border-gray-300 hover:bg-gray-200'
                  }`}
                >
                  {isListening ? <MicOff className="h-5 w-5" /> : <Mic className="h-5 w-5" />}
                </button>
                <button
                  onClick={handleSendMessage}
                  disabled={!inputValue.trim() || isLoading}
                  className="p-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  <Send className="h-5 w-5" />
                </button>
                <button
                  onClick={handleTestConnection}
                  className="p-3 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
                >
                  Test Connection
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AskEmma;
