using System.ComponentModel.DataAnnotations;

namespace Emma.Api.Dtos
{
    public class AskEmmaRequestDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? InteractionId { get; set; }
    }
}
