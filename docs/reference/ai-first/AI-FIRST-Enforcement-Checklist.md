# AI FIRST Enforcement Checklist

## For Jira Tickets / User Stories

### Story Template
```
As a [user type], I want to [accomplish goal] using conversational AI interface, so that I can [benefit].

ACCEPTANCE CRITERIA:
□ Primary interaction is conversational/natural language
□ AI agent provides intelligent recommendations
□ User can override AI decisions with explanation
□ All actions are auditable with reasoning
□ Fallback exists for AI service unavailability

AI FIRST REQUIREMENTS:
□ Feature supports "Ask EMMA" conversational interface
□ Context-aware recommendations using RAG
□ Explainable AI decisions with reasoning
□ User review and override capabilities
□ Comprehensive audit logging
```

### Definition of Done - AI FIRST Compliance
- [ ] **Conversational Interface**: Feature accessible via natural language commands
- [ ] **AI Agent Integration**: Core logic powered by AI agents (not just rules)
- [ ] **Context Awareness**: Uses comprehensive context for intelligent decisions
- [ ] **Explainable Actions**: All AI recommendations include reasoning
- [ ] **User Control**: Users can review, accept, or override AI suggestions
- [ ] **Audit Trail**: All AI actions logged with trace IDs and context
- [ ] **Error Handling**: Graceful fallback when AI services unavailable
- [ ] **Performance**: AI operations complete within 3 seconds for simple queries
- [ ] **Security**: AI context properly filtered for privacy compliance

## Code Review Checklist

### Architecture Compliance
- [ ] **Agent-First Pattern**: New controllers inherit from `AIFirstControllerBase`
- [ ] **Conversational Endpoints**: All major features expose `/ask` endpoint
- [ ] **Context Integration**: Uses `AIContextBuilder` for comprehensive context
- [ ] **Orchestration**: Complex workflows use `IAgentOrchestrator`
- [ ] **Service Abstraction**: AI calls go through `IAIFoundryService` interface

### Implementation Quality
- [ ] **Error Handling**: Try-catch blocks for all AI service calls
- [ ] **Logging**: Structured logging with trace IDs for AI operations
- [ ] **Validation**: Input validation before AI processing
- [ ] **Caching**: Appropriate caching for expensive AI operations
- [ ] **Rate Limiting**: Protection against AI service abuse

### Code Examples to Look For
```csharp
// GOOD: AI-First Controller Pattern
[HttpPost("ask")]
public async Task<IActionResult> Ask([FromBody] string query)
{
    var context = await _contextBuilder.BuildContextAsync(UserId, "contact_management");
    var response = await _orchestrator.ProcessRequestAsync(query, context);
    return Ok(response);
}

// BAD: Traditional-only endpoint without AI option
[HttpPost("update")]
public async Task<IActionResult> Update([FromBody] UpdateRequest request)
{
    // No AI integration, no conversational option
    await _service.UpdateAsync(request);
    return Ok();
}
```

## UX Review Checklist

### User Experience Standards
- [ ] **Conversational Flow**: Primary user journey uses chat/natural language
- [ ] **Progressive Disclosure**: Complex features accessible via simple commands
- [ ] **Feedback Loops**: Users can rate AI recommendations and provide feedback
- [ ] **Accessibility**: Conversational interface works with screen readers
- [ ] **Mobile Optimization**: Chat interface optimized for mobile devices
- [ ] **Loading States**: Clear indicators during AI processing
- [ ] **Error Messages**: Helpful error messages when AI fails

### Interaction Patterns
- [ ] **Natural Language Input**: Text input accepts conversational commands
- [ ] **Suggestion Chips**: Quick action buttons for common requests
- [ ] **Explanation on Demand**: "Why did you suggest this?" functionality
- [ ] **Undo/Redo**: Easy reversal of AI-driven actions
- [ ] **Context Display**: Show relevant context used for AI decisions

## Testing Requirements

### Unit Tests
- [ ] **AI Agent Tests**: Mock AI service responses for consistent testing
- [ ] **Context Building**: Test context gathering with various scenarios
- [ ] **Error Scenarios**: Test fallback behavior when AI services fail
- [ ] **Validation Logic**: Test input validation and sanitization
- [ ] **Response Parsing**: Test AI response parsing and error handling

### Integration Tests
- [ ] **End-to-End Flows**: Test complete conversational workflows
- [ ] **AI Service Integration**: Test actual AI service calls (with test data)
- [ ] **Context Accuracy**: Verify correct context is passed to AI services
- [ ] **Performance**: Test response times under various loads
- [ ] **Fallback Mechanisms**: Test graceful degradation scenarios

### Example Test Structure
```csharp
[Test]
public async Task Ask_ContactManagement_ReturnsAIResponse()
{
    // Arrange
    var query = "Show me all prospects in Toronto";
    var expectedContext = new AIContext { /* test context */ };
    _mockContextBuilder.Setup(x => x.BuildContextAsync(It.IsAny<Guid>(), "contact_management"))
                      .ReturnsAsync(expectedContext);
    
    // Act
    var result = await _controller.Ask(query);
    
    // Assert
    Assert.IsType<OkObjectResult>(result);
    _mockOrchestrator.Verify(x => x.ProcessRequestAsync(query, expectedContext), Times.Once);
}
```

## Performance Standards

### Response Time Requirements
- **Simple Queries**: < 2 seconds (e.g., "Show my contacts")
- **Complex Analysis**: < 5 seconds (e.g., "Analyze my pipeline")
- **Data Operations**: < 10 seconds (e.g., "Import and clean this CSV")
- **Batch Processing**: Progress indicators for operations > 10 seconds

### Resource Usage
- **Memory**: AI context should not exceed 10MB per request
- **CPU**: AI operations should not block other system functions
- **Network**: Implement request batching for multiple AI calls
- **Storage**: Cache frequently used AI responses appropriately

## Security Checklist

### Data Protection
- [ ] **PII Filtering**: Remove sensitive data from AI context
- [ ] **Access Control**: Verify user permissions before AI processing
- [ ] **Data Encryption**: Encrypt AI context in transit and at rest
- [ ] **Audit Logging**: Log all AI operations with user attribution
- [ ] **Rate Limiting**: Prevent abuse of AI services

### Compliance Requirements
- [ ] **GDPR**: Right to explanation for AI decisions
- [ ] **Industry Regulations**: Compliance rules integrated into AI logic
- [ ] **Data Retention**: AI logs follow data retention policies
- [ ] **Consent Management**: User consent for AI processing
- [ ] **Bias Detection**: Monitor AI decisions for potential bias

## Deployment Checklist

### Pre-Deployment
- [ ] **AI Service Connectivity**: Verify Azure AI Foundry connection
- [ ] **Configuration**: Validate AI service configuration and keys
- [ ] **Feature Flags**: AI features controlled by feature flags
- [ ] **Monitoring**: AI-specific monitoring and alerting configured
- [ ] **Documentation**: User documentation for new AI features

### Post-Deployment
- [ ] **Health Checks**: AI service health monitoring active
- [ ] **Performance Monitoring**: Track AI response times and success rates
- [ ] **User Feedback**: Collect and analyze user feedback on AI features
- [ ] **Error Tracking**: Monitor and alert on AI service errors
- [ ] **Usage Analytics**: Track adoption of conversational features

## Metrics and KPIs

### Technical Metrics
- **AI Utilization Rate**: % of requests processed via AI agents
- **Response Time**: Average time for AI-powered operations
- **Success Rate**: % of AI requests that complete successfully
- **Context Accuracy**: % of AI decisions using complete context
- **Fallback Rate**: % of requests that fall back to traditional processing

### Business Metrics
- **User Adoption**: % of active users using conversational features
- **Task Completion**: % of user goals achieved via AI interface
- **User Satisfaction**: NPS scores for AI-powered features
- **Productivity**: Time saved through AI automation
- **Accuracy**: Reduction in user errors through AI assistance

## Common Anti-Patterns to Avoid

### ❌ AI as Afterthought
```csharp
// BAD: Traditional endpoint with AI "sprinkled on"
public async Task<IActionResult> GetContacts()
{
    var contacts = await _service.GetContactsAsync();
    // Maybe add some AI suggestions as a side feature
    return Ok(contacts);
}
```

### ✅ AI-First Design
```csharp
// GOOD: AI-driven with traditional fallback
public async Task<IActionResult> Ask(string query)
{
    var aiResponse = await _orchestrator.ProcessRequestAsync(query);
    if (aiResponse.RequiresTraditionalFallback)
    {
        return await HandleTraditionalRequest(aiResponse.ParsedIntent);
    }
    return Ok(aiResponse);
}
```

### ❌ Hardcoded Business Logic
```csharp
// BAD: Static rules that can't adapt
if (contact.LastContactDate < DateTime.Now.AddDays(-30))
{
    return "Send follow-up email";
}
```

### ✅ AI-Driven Logic
```csharp
// GOOD: Context-aware AI recommendations
var context = await _contextBuilder.BuildContextAsync(contactId);
var recommendation = await _nbaAgent.GetRecommendationAsync(context);
return recommendation;
```

---

**This checklist should be used for all EMMA feature development to ensure AI FIRST compliance.**
