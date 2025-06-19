# 🔐 EMMA Demo Environment Backup

## Critical Configuration Files

### 1. Environment Variables (.env)
**Location**: `/.env`
**Status**: ✅ Contains working Azure OpenAI credentials
**Purpose**: AI service configuration, database connections

### 2. Frontend Environment
**Location**: `/Emma.Frontend/.env` (if exists)
**React Environment Variables**:
```
REACT_APP_API_BASE_URL=http://localhost:5000
REACT_APP_AZURE_OPENAI_API_KEY=[configured]
REACT_APP_AZURE_OPENAI_ENDPOINT=[configured]
REACT_APP_AZURE_OPENAI_DEPLOYMENT=gpt-4.1
REACT_APP_AZURE_OPENAI_MODEL=gpt-4
REACT_APP_AZURE_OPENAI_MAX_TOKENS=1000
REACT_APP_AZURE_OPENAI_TEMPERATURE=0.7
```

### 3. Backend Configuration
**Location**: `/Emma.Api/Program.cs`
**Key Settings**:
- CORS policy: "AllowReactFrontend" for localhost:3000
- URL binding: http://localhost:5000
- Azure OpenAI service registration
- Database context with fallback handling

### 4. AI Service Configuration
**Location**: `/Emma.Frontend/src/services/aiService.js`
**Key Features**:
- Dynamic context fetching from /api/contacts
- Robust fallback with real seed data
- Conversation history handling
- Error handling and logging

## 🎯 Working State Snapshot

### Services Running
- **Backend**: localhost:5000 (with CORS)
- **Frontend**: localhost:3000
- **AI**: Azure OpenAI GPT-4 fully functional

### Data Integration
- **Primary**: Attempts to fetch from /api/contacts
- **Fallback**: Real seed data structure with actual contacts
- **Contacts**: Emily Johnson, Robert Williams, Chris Gabriel, Kevin Brown, Lisa Chang, Sarah Davis

### Dependencies Installed
- **Frontend**: React 18, Azure OpenAI client
- **Backend**: .NET 8, Entity Framework, Azure OpenAI SDK

## 🔄 Restoration Steps

1. **Checkout Git Tag**: `git checkout demo-v1.0-working`
2. **Restore Dependencies**: `npm install` + `dotnet restore`
3. **Verify .env File**: Ensure Azure OpenAI credentials present
4. **Start Services**: Backend first, then frontend
5. **Test AI Feature**: "Ask EMMA Anything" should work immediately

## 📋 Verification Checklist

- [ ] Git tag `demo-v1.0-working` exists
- [ ] .env file contains Azure OpenAI credentials
- [ ] CORS configured in Program.cs
- [ ] aiService.js has enhanced fallback
- [ ] Both servers start without errors
- [ ] AI responses reference real contact names
- [ ] No CORS errors in browser console

---
**Backup Created**: June 6, 2025  
**Demo Status**: ✅ Fully Functional  
**Restoration Time**: ~5 minutes
