using PracticeAgent.Models;
using Microsoft.SemanticKernel;

namespace PracticeAgent.Interfaces
{
    public interface IPractibotAgent
    {
        Task<ChatResponse> Chat(string message, ChatDeployment core);
        Task<string> AcknowledgeAudioMessage(KernelArguments args, ChatDeployment chat);
        string InitAgent();
    }
}