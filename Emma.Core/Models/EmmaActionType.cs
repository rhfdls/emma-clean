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
    Unknown
}
