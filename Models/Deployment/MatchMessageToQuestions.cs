using System.Text.Json.Serialization;

namespace PracticeAgent.Models
{
    public class MatchMessageToQuestions
    {
        public Survey survey { get; set; } = new Survey();
        public string message { get; set; } = string.Empty;
        public bool success { get; set; } = false;
    }
}