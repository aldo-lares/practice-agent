using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OLSOrca.Models;
using OLSOrca.Services.Interfaces;
using System.Text;

namespace OLSOrca.Services
{
    public class ChatService : IChatService
    {   
        private readonly ICosmosService _cosmosService;

        public ChatService(ICosmosService cosmosService)
        {
            _cosmosService = cosmosService ?? throw new ArgumentNullException(nameof(cosmosService));
        }

        public async Task<ChatDeployment> GetChatAsync(string sessionId)
        {    
            List<ChatDeployment> response = await _cosmosService.ReadItemAsync<ChatDeployment>("chat", sessionId);
            if (response == null || response.Count == 0)
            {  
                ChatDeployment chat = new ChatDeployment();
                chat.id = sessionId;
                await _cosmosService.UpsertItemAsync("chat", chat, sessionId);
                return chat;
            }
            else
            {
               return response.FirstOrDefault() ?? new ChatDeployment();
            }
        }

        public async Task<bool> UpdateChatAsync(ChatDeployment core)
        {
            if (core == null)
                throw new ArgumentNullException(nameof(core));
                
            return await _cosmosService.UpsertItemAsync("chat", core, core.id);
        }

        public async Task<bool> PutMessageAsync(ChatDeployment chat, string message, AuthorRole role)
        {
            if (chat == null)
                throw new ArgumentNullException(nameof(chat));
                
            if (string.IsNullOrEmpty(message))
                return false;
            
            chat.Messages.Add(new SimpleChatMessage
            {
                Role = role.ToString(),
                Content = message,
            });
            return await UpdateChatAsync(chat);
        }

        /// <summary>
        /// Retrieves context from chat history by formatting the last n messages
        /// </summary>
        /// <param name="chat">The chat deployment containing messages</param>
        /// <param name="messageCount">Number of recent messages to include</param>
        /// <returns>A string containing formatted messages in "role: content" format</returns>
        public string GetChatContext(ChatDeployment chat, int messageCount)
        {
            if (chat == null || chat.Messages == null || chat.Messages.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            int startIndex = Math.Max(0, chat.Messages.Count - messageCount);
            
            for (int i = startIndex; i < chat.Messages.Count; i++)
            {
                var message = chat.Messages[i];
                sb.AppendLine($"{message.Role}: {message.Content}");
            }
            
            return sb.ToString().TrimEnd();
        }
    }
}