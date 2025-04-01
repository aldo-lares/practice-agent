namespace PracticeAgent.Models
{
    public class APIResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? Transcription { get; set; } // Added to store transcription text
        
        // New properties for survey progress
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public int Progress { get; set; }
    }
}