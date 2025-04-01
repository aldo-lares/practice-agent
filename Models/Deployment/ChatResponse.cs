namespace PracticeAgent.Models
{
    public class ChatResponse
    {
        public required ChatDeployment Chat { get; set; }
        public string Message { get; set; } = string.Empty;

        public bool Success { get; set; } = false;
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
        public int Progress { get; set; }
    }
}