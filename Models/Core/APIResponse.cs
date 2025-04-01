namespace PracticeAgent.Models
{
    public class APIResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? Transcription { get; set; } // Added to store transcription text
    }
}