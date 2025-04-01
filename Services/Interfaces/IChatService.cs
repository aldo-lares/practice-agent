using Microsoft.SemanticKernel.ChatCompletion;
using PracticeAgent.Models;

namespace PracticeAgent.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatDeployment> GetChatAsync(string sessionId);
        Task<bool> UpdateChatAsync(ChatDeployment core);
        Task<bool> PutMessageAsync(ChatDeployment core, string message, AuthorRole role);
        string GetChatContext(ChatDeployment chat, int messageCount);

    }
}
