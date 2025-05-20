using System.Threading.Tasks;
using System.Collections.Generic;

namespace Emma.Core
{
    public interface IEmmaAnalyzer
    {
        Task<EmmaAnalysisResult> AnalyzeTranscriptionAsync(string transcription, CancellationToken cancellationToken = default);
        Task<IEnumerable<EmmaTask>> GenerateWorkflowAsync(EmmaAnalysisResult analysis);
        Task<EmmaInsight> GenerateInsightsAsync(EmmaAnalysisResult analysis);
    }

    public class EmmaAnalysisResult
    {
        public string Transcription { get; set; }
        public IEnumerable<EmmaTask> Tasks { get; set; }
        public EmmaInsight Insights { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public class EmmaTask
    {
        public string TaskId { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public double Priority { get; set; }
        public string AssignedTo { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class EmmaInsight
    {
        public string Summary { get; set; }
        public string Sentiment { get; set; }
        public double SentimentScore { get; set; }
        public IEnumerable<string> KeyPoints { get; set; }
        public IEnumerable<string> ActionItems { get; set; }
        public string NextSteps { get; set; }
    }
}
