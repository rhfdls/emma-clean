using System;
using System.ComponentModel.DataAnnotations;

namespace Emma.Api.Dtos
{
    public class AskEmmaRequestDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? InteractionId { get; set; }
        
        /// <summary>
        /// Organization ID to determine industry-specific context and prompts
        /// </summary>
        public Guid? OrganizationId { get; set; }
        
        /// <summary>
        /// Optional industry code override (if not using organization's default)
        /// </summary>
        public string? IndustryCode { get; set; }
    }
}
