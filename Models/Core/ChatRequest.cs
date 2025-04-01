namespace PracticeAgent.Models
{
    public class ChatRequest
    {
        public string UserMessage { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }
}