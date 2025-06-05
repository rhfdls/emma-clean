namespace Emma.Api.Services;

public static class PromptTemplates
{
    public const string InteractionAnalyzer = @"
You are Emma's Interaction Analyzer. Analyze this client interaction and extract key insights.

INTERACTION TEXT:
{interaction_text}

CLIENT CONTEXT:
{client_context}

Analyze and respond with ONLY valid JSON in this exact format:
{{
  ""summary"": ""Brief 2-sentence summary of the interaction"",
  ""sentiment"": {{
    ""label"": ""positive"" | ""neutral"" | ""negative"",
    ""score"": 0.0 to 1.0
  }},
  ""key_topics"": [""topic1"", ""topic2"", ""topic3""],
  ""entities"": {{
    ""people"": [""names mentioned""],
    ""companies"": [""company names""],
    ""dates"": [""important dates""],
    ""amounts"": [""monetary amounts or quantities""]
  }},
  ""intent"": ""What the client wants or needs"",
  ""urgency"": ""low"" | ""medium"" | ""high"",
  ""next_action_hints"": [""suggested follow-up action 1"", ""suggested follow-up action 2""]
}}";

    public const string NbaRecommender = @"
You are Emma's Next Best Action Recommender. Based on client context, recommend the most effective next action.

CLIENT SUMMARY:
{client_summary}

RECENT INTERACTIONS:
{recent_interactions}

BUSINESS CONTEXT:
- Deal Stage: {deal_stage}
- Timeline: {timeline}
- Budget Indicators: {budget_indicators}

Respond with ONLY valid JSON in this exact format:
{{
  ""primary_recommendation"": {{
    ""action"": ""email"" | ""call"" | ""meeting"" | ""proposal"" | ""demo"" | ""follow_up"",
    ""priority"": ""high"" | ""medium"" | ""low"",
    ""timing"": ""immediate"" | ""this_week"" | ""next_week"" | ""this_month""
  }},
  ""reasoning"": ""Clear explanation of why this action is best"",
  ""message_suggestions"": [
    ""Talking point or message suggestion 1"",
    ""Talking point or message suggestion 2"",
    ""Talking point or message suggestion 3""
  ],
  ""alternative_actions"": [
    {{
      ""action"": ""alternative action type"",
      ""reasoning"": ""Why this could also work""
    }}
  ],
  ""success_metrics"": [""How to measure if this action was effective""]
}}";

    public const string ClientSummarizer = @"
You are Emma's Client Journey Summarizer. Create a concise, actionable summary of the client relationship.

PREVIOUS SUMMARY:
{existing_summary}

NEW INTERACTIONS:
{new_interactions}

CLIENT PROFILE:
{client_profile}

Respond with ONLY valid JSON in this exact format:
{{
  ""relationship_stage"": ""prospect"" | ""qualified"" | ""evaluation"" | ""negotiation"" | ""active"" | ""at_risk"" | ""champion"",
  ""key_interests"": [""main business interests or pain points""],
  ""communication_style"": ""formal"" | ""casual"" | ""technical"" | ""relationship_focused"",
  ""decision_timeline"": ""immediate"" | ""short_term"" | ""long_term"" | ""unknown"",
  ""budget_indicators"": ""strong"" | ""moderate"" | ""limited"" | ""unknown"",
  ""relationship_health"": ""excellent"" | ""good"" | ""concerning"" | ""critical"",
  ""summary"": ""3-sentence relationship overview focusing on current status and opportunities""
}}";
}
