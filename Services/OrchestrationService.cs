using Microsoft.Extensions.Configuration;
using PracticeAgent.Interfaces;
using PracticeAgent.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Microsoft.SemanticKernel;
using System.Text;
using PracticeAgent.Utilities;
using PracticeAgent.Services.Interfaces;
using PracticeAgent.SemanticPlugins;
using Microsoft.Extensions.Logging;

namespace PracticeAgent.Services
{
    /// <summary>
    /// Service responsible for orchestrating chat interactions between users and AI agents
    /// </summary>
    public class OrchestrationService
    {
        private readonly IPractibotAgent _agent;
        private readonly IChatService _chatService;
        private readonly ISurveyService _surveyService;
        private readonly IAudioTranscriptionService _transcriptionService;
        private readonly ILogger<OrchestrationService> _logger;

        public OrchestrationService(
            IPractibotAgent agent,
            IChatService chatService,
            ISurveyService surveyService,
            IAudioTranscriptionService transcriptionService,
            ILogger<OrchestrationService> logger)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _surveyService = surveyService ?? throw new ArgumentNullException(nameof(surveyService));
            _transcriptionService = transcriptionService ?? throw new ArgumentNullException(nameof(transcriptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Process a user's chat message and get a response from the AI agent
        /// </summary>
        /// <param name="sessionId">Unique identifier for the chat session</param>
        /// <param name="userMessage">The message sent by the user</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>API response containing the agent's reply</returns>
        public async Task<APIResponse> Chat(string sessionId, string userMessage, CancellationToken cancellationToken = default)
        {
            ValidateInputs(sessionId, userMessage);

            try
            {
                var stringBuilder = new StringBuilder();
                // Retrieve or create chat session
                var chat = await _chatService.GetChatAsync(sessionId);
                
                // Add the user message to the chat history
                await _chatService.PutMessageAsync(chat, userMessage, AuthorRole.User);

                // Initialize agent if this is a new conversation
                if (ShouldInitializeAgent(chat))
                    stringBuilder.AppendLine(StringUtilities.AppendBr(await InitializeAgentAsync(chat)));

                // Get response from agent
                var response = await _agent.Chat(userMessage, chat);
                stringBuilder.AppendLine(StringUtilities.AppendBr(response.Message));
                
                // Record agent's response in chat history
                await _chatService.PutMessageAsync(response.Chat, response.Message, AuthorRole.Assistant);

                // Return formatted response
                return new APIResponse
                {
                    Response = stringBuilder.ToString(),
                    Success = response.Success,
                    TotalQuestions = response.TotalQuestions,
                    AnsweredQuestions = response.AnsweredQuestions,
                    Progress = response.Progress,
                };
            }
            catch (Exception ex)
            {
                // Log exception here
                return new APIResponse
                {
                    Response = $"An error occurred processing your request: {ex.Message}",
                    Success = false
                };
            }
        }

        /// <summary>
        /// Process an audio message from the user and get a response from the AI agent
        /// </summary>
        /// <param name="sessionId">Unique identifier for the chat session</param>
        /// <param name="audioData">Base64 encoded audio data</param>
        /// <param name="audioFormat">Format of the audio (e.g., mp3, wav)</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>API response containing the agent's reply</returns>
        public async Task<APIResponse> ProcessAudio(string sessionId, string audioData, string audioFormat, CancellationToken cancellationToken = default)
        {
            // Input validation
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
            
            if (string.IsNullOrEmpty(audioData))
                throw new ArgumentException("Audio data cannot be null or empty", nameof(audioData));
            
            var stringBuilder = new StringBuilder();
            ChatDeployment chat = null;
            
            try
            {
                _logger.LogInformation("Transcribing audio message for session {SessionId}", sessionId);
                
                // Attempt transcription
                string transcribedText = await TranscribeAudioSafelyAsync(audioData, audioFormat, chat);
                
                // If we have valid transcribed text, process it as a normal chat message
                if (!string.IsNullOrEmpty(transcribedText))
                {
                    return await Chat(sessionId, transcribedText, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio for session {SessionId}", sessionId);
                
                // Try to add the error to chat history if we have a chat object
                if (chat != null)
                {
                    try
                    {
                        string errorMessage = $"[Error processing audio: {ex.Message}]";
                        await _chatService.PutMessageAsync(chat, errorMessage, AuthorRole.System);
                    }
                    catch (Exception chatEx)
                    {
                        _logger.LogError(chatEx, "Failed to record error in chat history for session {SessionId}", sessionId);
                    }
                }
                
                return new APIResponse
                {
                    Response = $"An error occurred processing your audio request: {ex.Message}",
                    Success = false
                };
            }
            
            // If execution reaches here, it means transcription returned empty but didn't throw an exception
            return new APIResponse
            {
                Response = "We received your audio message, but couldn't understand it. Please try again or type your message.",
                Success = false
            };
        }

        /// <summary>
        /// Attempts to transcribe audio safely, handling errors and empty results
        /// </summary>
        private async Task<string> TranscribeAudioSafelyAsync(string audioData, string audioFormat, ChatDeployment chat)
        {
            try
            {
                // Transcribe the audio using Whisper
                string transcribedText = await _transcriptionService.TranscribeAsync(audioData, audioFormat, "es-MX");
                
                if (string.IsNullOrEmpty(transcribedText))
                {
                    _logger.LogWarning("Audio transcription returned empty text");
                    
                    // Add a placeholder message to the chat history
                    string placeholderMessage = $"[Audio message received but could not be transcribed, format: {audioFormat}]";
                    await _chatService.PutMessageAsync(chat, placeholderMessage, AuthorRole.User);
                    
                    // Handle empty transcription
                    await HandleEmptyTranscription(chat, audioFormat);
                    return null;
                }
                
                _logger.LogInformation("Audio successfully transcribed");
                return transcribedText;
            }
            catch (Exception ex)
            {
                // Record the transcription error in the chat history
                string errorMessage = $"[Audio message received but transcription failed: {ex.Message}]";
                await _chatService.PutMessageAsync(chat, errorMessage, AuthorRole.User);
                _logger.LogError(ex, "Transcription failed");
                
                // Handle transcription failure
                await HandleEmptyTranscription(chat, audioFormat);
                return null;
            }
        }

        /// <summary>
        /// Handles the case when transcription fails or returns empty
        /// </summary>
        private async Task HandleEmptyTranscription(ChatDeployment chat, string audioFormat)
        {
            // Acknowledge receipt of audio message
            var response = await AcknowledgeAudioMessage(chat, audioFormat);
            
            // Record agent's response in chat history
            await _chatService.PutMessageAsync(response.Chat, response.Message, AuthorRole.Assistant);
        }

        /// <summary>
        /// Get an acknowledgment response for an audio message
        /// </summary>
        private async Task<ChatResponse> AcknowledgeAudioMessage(ChatDeployment chat, string audioFormat)
        {
            KernelArguments args = new KernelArguments { ["audioFormat"] = audioFormat };
            string acknowledgeMessage = await _agent.AcknowledgeAudioMessage(args, chat);
            return new ChatResponse { Chat = chat, Message = acknowledgeMessage, Success = true };
        }

        /// <summary>
        /// Validates input parameters
        /// </summary>
        private void ValidateInputs(string sessionId, string userMessage)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
            
            if (string.IsNullOrEmpty(userMessage))
                throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));
        }

        /// <summary>
        /// Determines if the agent needs to be initialized for this chat
        /// </summary>
        private bool ShouldInitializeAgent(ChatDeployment chat)
        {
            return chat.Messages == null || chat.Messages.Count <= 1;
        }

        /// <summary>
        /// Initializes the agent for a new conversation if needed
        /// </summary>
        private async Task<string> InitializeAgentAsync(ChatDeployment chat)
        {
            string init = _agent.InitAgent();
            Survey survey = _surveyService.LoadSurveyFromFile();
            chat.TotalQuestions = survey.questions.Count;
            chat.Survey = survey;
            await _chatService.PutMessageAsync(chat, init, AuthorRole.Assistant);
            return init;
        }
    }
}