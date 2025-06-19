using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Emma.Core.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmmaActionType
{
    [EnumMember(Value = "sendemail")]
    SendEmail,
    
    [EnumMember(Value = "schedulefollowup")]
    ScheduleFollowup,
    
    [EnumMember(Value = "none")]
    None,
    
    [EnumMember(Value = "unknown")]
    Unknown,
    
    // Agent Orchestration Actions
    [EnumMember(Value = "classifycontact")]
    ClassifyContact,
    
    [EnumMember(Value = "createcontact")]
    CreateContact,
    
    [EnumMember(Value = "retrievecontext")]
    RetrieveContext,
    
    [EnumMember(Value = "analyzesentiment")]
    AnalyzeSentiment,
    
    [EnumMember(Value = "escalateurgent")]
    EscalateUrgent,
    
    [EnumMember(Value = "synctoCrm")]
    SyncToCrm,
    
    [EnumMember(Value = "excludefrombusiness")]
    ExcludeFromBusiness,
    
    [EnumMember(Value = "promptforinfo")]
    PromptForInfo,
    
    [EnumMember(Value = "triggerworkflow")]
    TriggerWorkflow,
    
    [EnumMember(Value = "updatecontactstate")]
    UpdateContactState
}
