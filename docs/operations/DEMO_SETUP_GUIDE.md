# 🎯 EMMA AI Demo Setup Guide

## Quick Start (5 minutes)

This snapshot contains a **fully functional EMMA AI demo** with real data integration.

### Prerequisites
- Node.js 18+ installed
- .NET 8 SDK installed
- Git installed

### 1. Clone & Setup
```bash
git clone <your-repo-url>
cd emma
git checkout demo-v1.0-working
```

### 2. Install Dependencies
```bash
# Frontend
cd Emma.Frontend
npm install

# Backend (restore packages)
cd ../Emma.Api
dotnet restore
```

### 3. Start Demo (2 terminals)

**Terminal 1 - Backend API:**
```bash
cd Emma.Api
dotnet run --urls "http://localhost:5000"
```

**Terminal 2 - Frontend React:**
```bash
cd Emma.Frontend
npm start
```

### 4. Access Demo
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger

## ✅ Demo Features Working

### AI Integration
- **Ask EMMA Anything** feature fully functional
- Real contact data integration (Emily Johnson, Robert Williams, Chris Gabriel, etc.)
- Azure OpenAI GPT-4 responses with business context
- Robust fallback ensures demo always works

### Contact Data
- **Active Clients**: Emily Johnson, Robert Williams, Chris Gabriel
- **Prospects**: Kevin Brown, Lisa Chang  
- **Leads**: Sarah Davis
- Complete contact details, budgets, and tags

### Technical Stack
- **Backend**: .NET 8 Web API with CORS configured
- **Frontend**: React 18 with Azure OpenAI integration
- **AI**: Azure OpenAI GPT-4 with dynamic context
- **Data**: Real estate seed data with fallback support

## 🎬 Demo Script

### Sample Questions for EMMA:
1. "Who are my active clients?"
2. "What's Emily Johnson's budget?"
3. "Tell me about Chris Gabriel's investment property"
4. "What should I follow up on with Robert Williams?"
5. "Show me my prospects and their status"

### Expected AI Responses:
- References specific contact names and details
- Provides actionable real estate advice
- Includes market insights and next steps
- Contextual responses based on contact tags and budgets

## 🔧 Configuration

### Environment Variables (.env)
All Azure OpenAI credentials are pre-configured in `.env` file.

### CORS Configuration
Backend configured to allow React frontend calls from localhost:3000.

### Fallback Data
AI service includes complete fallback with real seed data structure.

## 🚀 Deployment Notes

This demo is configured for local development. For production:
1. Update CORS origins in Program.cs
2. Configure proper database connection
3. Enable privacy enforcement services
4. Add proper authentication (JWT/OIDC)

## 📞 Support

This snapshot preserves the exact working state as of the demo creation.
All features are tested and functional for immediate demonstration use.

---
**Demo Version**: v1.0  
**Last Updated**: June 6, 2025  
**Status**: ✅ Fully Functional
