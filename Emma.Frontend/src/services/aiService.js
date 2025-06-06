import OpenAI from 'openai';

// Initialize Azure OpenAI client
const openai = new OpenAI({
  apiKey: process.env.REACT_APP_AZURE_OPENAI_API_KEY,
  baseURL: `${process.env.REACT_APP_AZURE_OPENAI_ENDPOINT}openai/deployments/${process.env.REACT_APP_AZURE_OPENAI_DEPLOYMENT}`,
  defaultQuery: { 'api-version': '2024-02-15-preview' },
  defaultHeaders: {
    'api-key': process.env.REACT_APP_AZURE_OPENAI_API_KEY,
  },
  dangerouslyAllowBrowser: true // Note: In production, API calls should go through your backend
});

// EMMA's context about the real estate business
const EMMA_CONTEXT = `You are EMMA, an AI assistant for a real estate agent. You have access to the following data:

ACTIVE CONTACTS:
- Chris Gabriel: Prospect from Toronto, had property inspection on Nov 20th. Deciding between property needing $1,500-2,000 repairs vs move-in ready option with higher fees. URGENT follow-up needed.
- Emily Johnson: Active client since Oct 15th, first-time buyer, pre-approved, shopping for condos in San Diego. Contact: emily.johnson@gmail.com, +1-619-555-0123
- Robert Williams: Active luxury client from La Jolla, budget $800K-1.2M, in bidding war. Contact: rwilliams@techcorp.com, +1-858-555-0456
- Amanda Foster: Hot prospect, luxury condos downtown, budget $600K-800K, viewing scheduled
- Marcus Thompson: Investment buyer, needs completed property analysis, cash buyer

MARKET DATA:
- San Diego home values up 8% year-over-year
- Average days on market: 25-30 days
- Low inventory levels favoring sellers
- Downtown condos: $600K-1.2M, La Jolla luxury: $800K-2M+

RECENT INSIGHTS:
- 60% of inspections find HVAC issues (avg age 12+ years)
- Average immediate repair costs: $2,500-4,000
- 40% of prospects face competing offers
- Conversion rate: 68% (above industry 45%)
- Average response time: 2.3 hours

Always provide specific, actionable real estate advice based on this context. Include relevant contact details, market insights, and next steps when appropriate.`;

export const aiService = {
  async askEmma(question, conversationHistory = []) {
    try {
      // Build conversation context
      const messages = [
        {
          role: 'system',
          content: EMMA_CONTEXT
        },
        // Include recent conversation history for context
        ...conversationHistory.slice(-6).map(msg => ({
          role: msg.type === 'user' ? 'user' : 'assistant',
          content: msg.content
        })),
        {
          role: 'user',
          content: question
        }
      ];

      const response = await openai.chat.completions.create({
        model: process.env.REACT_APP_AZURE_OPENAI_MODEL, // Use actual model name for Azure OpenAI
        messages: messages,
        max_tokens: parseInt(process.env.REACT_APP_AZURE_OPENAI_MAX_TOKENS) || 1000,
        temperature: parseFloat(process.env.REACT_APP_AZURE_OPENAI_TEMPERATURE) || 0.7,
      });

      const aiResponse = response.choices[0].message.content;
      
      // Generate insights based on the response content
      const insights = this.generateInsights(aiResponse, question);

      return {
        content: aiResponse,
        insights: insights,
        usage: response.usage
      };

    } catch (error) {
      console.error('AI Service Error:', error);
      
      // Fallback to mock response if AI fails
      return this.getFallbackResponse(question);
    }
  },

  generateInsights(response, question) {
    const insights = [];
    const lowerResponse = response.toLowerCase();
    const lowerQuestion = question.toLowerCase();

    // Analyze response content to generate relevant insights
    if (lowerResponse.includes('urgent') || lowerResponse.includes('immediately')) {
      insights.push({
        icon: 'AlertCircle',
        text: 'Urgent action required',
        color: 'text-red-600'
      });
    }

    if (lowerResponse.includes('follow-up') || lowerResponse.includes('call')) {
      insights.push({
        icon: 'Phone',
        text: 'Follow-up recommended',
        color: 'text-blue-600'
      });
    }

    if (lowerResponse.includes('market') || lowerResponse.includes('price') || lowerResponse.includes('%')) {
      insights.push({
        icon: 'TrendingUp',
        text: 'Market data included',
        color: 'text-green-600'
      });
    }

    if (lowerResponse.includes('schedule') || lowerResponse.includes('meeting') || lowerResponse.includes('appointment')) {
      insights.push({
        icon: 'Clock',
        text: 'Scheduling involved',
        color: 'text-orange-600'
      });
    }

    if (lowerQuestion.includes('chris') || lowerResponse.includes('chris gabriel')) {
      insights.push({
        icon: 'User',
        text: 'Chris Gabriel context',
        color: 'text-purple-600'
      });
    }

    // Default insight if none generated
    if (insights.length === 0) {
      insights.push({
        icon: 'Sparkles',
        text: 'AI-powered response',
        color: 'text-blue-600'
      });
    }

    return insights.slice(0, 3); // Limit to 3 insights
  },

  getFallbackResponse(question) {
    return {
      content: `I'm having trouble connecting to the AI service right now. However, I can still help you with information about your contacts and real estate business. 

Based on your question "${question}", you might want to know about:
• Chris Gabriel's urgent property decision follow-up
• Current market trends (8% appreciation in San Diego)
• Follow-up priorities for this week
• Property inspection insights and common issues

Please try rephrasing your question, or ask about specific contacts like Chris Gabriel, Emily Johnson, Robert Williams, Amanda Foster, or Marcus Thompson.`,
      insights: [
        {
          icon: 'AlertCircle',
          text: 'AI service temporarily unavailable',
          color: 'text-orange-600'
        },
        {
          icon: 'Users',
          text: 'Contact data still accessible',
          color: 'text-blue-600'
        }
      ]
    };
  },

  // Debug method to check configuration
  debug() {
    return {
      environmentVariables: {
        REACT_APP_AZURE_OPENAI_API_KEY: process.env.REACT_APP_AZURE_OPENAI_API_KEY ? 'SET' : 'NOT SET',
        REACT_APP_AZURE_OPENAI_ENDPOINT: process.env.REACT_APP_AZURE_OPENAI_ENDPOINT,
        REACT_APP_AZURE_OPENAI_DEPLOYMENT: process.env.REACT_APP_AZURE_OPENAI_DEPLOYMENT,
        REACT_APP_AZURE_OPENAI_MODEL: process.env.REACT_APP_AZURE_OPENAI_MODEL,
        REACT_APP_AZURE_OPENAI_MAX_TOKENS: process.env.REACT_APP_AZURE_OPENAI_MAX_TOKENS,
        REACT_APP_AZURE_OPENAI_TEMPERATURE: process.env.REACT_APP_AZURE_OPENAI_TEMPERATURE,
      },
      baseURL: `${process.env.REACT_APP_AZURE_OPENAI_ENDPOINT}openai/deployments/${process.env.REACT_APP_AZURE_OPENAI_DEPLOYMENT}`,
      timestamp: new Date().toISOString()
    };
  },

  // Test connection to AI service
  async testConnection() {
    try {
      const response = await openai.chat.completions.create({
        model: process.env.REACT_APP_AZURE_OPENAI_MODEL, // Use actual model name for Azure OpenAI
        messages: [{ role: 'user', content: 'Hello, this is a connection test.' }],
        max_tokens: 50
      });
      
      return {
        success: true,
        message: 'AI service connected successfully',
        model: process.env.REACT_APP_AZURE_OPENAI_MODEL,
        response: response.choices[0].message.content
      };
    } catch (error) {
      return {
        success: false,
        message: error.message,
        error: error
      };
    }
  }
};

export default aiService;
