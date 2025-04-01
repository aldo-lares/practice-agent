namespace OLSOrca.Models
{
    public class ChatResponse
    {
        public required ChatDeployment Chat { get; set; }
        public string Message { get; set; } = string.Empty;

        public bool Success { get; set; } = false;
    }
}