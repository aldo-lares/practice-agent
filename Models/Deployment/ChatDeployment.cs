using Microsoft.SemanticKernel.ChatCompletion;

namespace OLSOrca.Models
{
    public class ChatDeployment:ChatCore
    {
        public List<SimpleChatMessage> Messages { get; set; } = new List<SimpleChatMessage>();
        public Survey Survey { get; set; } = new Survey();
        public Question LastAskedQuestion { get; set; } = null;
        public Survey AnsweredSurvey { get; set; } = new Survey();

    }
}