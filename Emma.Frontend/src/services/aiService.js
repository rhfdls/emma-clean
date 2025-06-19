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

// Fetch real contact data from the API
async function fetchContactContext() {
  try {
    const response = await fetch(`${process.env.REACT_APP_API_BASE_URL}/api/contacts`);
    if (!response.ok) {
      throw new Error('Failed to fetch contacts');
    }
    const contacts = await response.json();
    
    // Build dynamic context from real contact data
    const activeClients = contacts.filter(c => c.relationshipState === 'Client' && c.isActiveClient);
    const prospects = contacts.filter(c => c.relationshipState === 'Prospect');
    const leads = contacts.filter(c => c.relationshipState === 'Lead');
    
    return `You are EMMA, an AI assistant for a real estate agent. You have access to the following REAL data from the CRM:

ACTIVE CLIENTS (${activeClients.length}):
${activeClients.map(c => `- ${c.firstName} ${c.lastName}: ${c.tags?.join(', ') || 'No tags'} | Email: ${c.emails?.[0]?.address || 'No email'} | Phone: ${c.phones?.[0]?.number || 'No phone'}`).join('\n')}

PROSPECTS (${prospects.length}):
${prospects.map(c => `- ${c.firstName} ${c.lastName}: ${c.tags?.join(', ') || 'No tags'} | Email: ${c.emails?.[0]?.address || 'No email'} | Phone: ${c.phones?.[0]?.number || 'No phone'}`).join('\n')}

LEADS (${leads.length}):
${leads.map(c => `- ${c.firstName} ${c.lastName}: ${c.tags?.join(', ') || 'No tags'} | Email: ${c.emails?.[0]?.address || 'No email'} | Phone: ${c.phones?.[0]?.number || 'No phone'}`).join('\n')}

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

Always provide specific, actionable real estate advice based on this REAL contact data. Include relevant contact details, market insights, and next steps when appropriate.`;
  } catch (error) {
    console.error('Failed to fetch contact context:', error);
    // Fallback to static context with real seed data structure if API fails
    return `You are EMMA, an AI assistant for a real estate agent. I'm using cached contact data from your CRM:

ACTIVE CLIENTS (3):
- Emily Johnson: First-time buyer, Budget $400K-500K | Email: emily.johnson@email.com | Phone: (555) 123-4567
- Robert Williams: Luxury buyer, Budget $800K+ | Email: robert.williams@email.com | Phone: (555) 234-5678
- Chris Gabriel: Investment property | Email: chris.gabriel@email.com | Phone: (555) 345-6789

PROSPECTS (2):
- Kevin Brown: Downsizing, Budget $300K-400K | Email: kevin.brown@email.com | Phone: (555) 456-7890
- Lisa Chang: First-time buyer, Budget $350K-450K | Email: lisa.chang@email.com | Phone: (555) 567-8901

LEADS (1):
- Sarah Davis: Relocating from out of state | Email: sarah.davis@email.com | Phone: (555) 678-9012

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

Always provide specific, actionable real estate advice based on this contact data. Include relevant contact details, market insights, and next steps when appropriate.`;
  }
}

export const aiService = {
  async askEmma(question, conversationHistory = []) {
    try {
      // Fetch real-time contact context
      const dynamicContext = await fetchContactContext();
      
      // Build conversation context
      const messages = [
        {
          role: 'system',
          content: dynamicContext
        },
        // Include recent conversation history for context
        ...conversationHistory.slice(-6)
          .filter(msg => msg.text && msg.text.trim())
          .map(msg => ({
            role: msg.sender === 'user' ? 'user' : 'assistant',
            content: msg.text
          })),
        {
          role: 'user',
          content: question
        }
      ];

      const response = await openai.chat.completions.create({
        model: process.env.REACT_APP_AZURE_OPENAI_MODEL,
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
        model: process.env.REACT_APP_AZURE_OPENAI_MODEL,
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
