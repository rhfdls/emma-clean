namespace Emma.Models.Models;

public class EmmaTask
{
    public int Id { get; set; } // Primary key
    public string TaskType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
