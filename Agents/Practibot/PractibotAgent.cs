using OLSOrca.Interfaces;
using OLSOrca.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Agents;
using OLSOrca.SemanticPlugins;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;
using System.Text;
using System.Drawing;
using OLSOrca.Utilities;
using OLSOrca.Services;
using OLSOrca.Services.Interfaces;


namespace OLSOrca.Plugins
{
    public class PractibotAgent : IPractibotAgent
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _deploymentName;

        private Kernel _kernel;
        private ChatCompletionAgent _agent;
        private IConfiguration _configuration;
        private readonly IChatService _chatService;


        public PractibotAgent(IConfiguration configuration, IChatService chatService)
        {

            _configuration = configuration;
            _chatService = chatService;
            _apiKey = configuration["AzureOpenAi:ApiKey"]??"";
            _endpoint = configuration["AzureOpenAi:Endpoint"]??"";
            _deploymentName = configuration["AzureOpenAi:DeploymentName"]??"";

            var KernelBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(_deploymentName,_endpoint,_apiKey);
            _kernel = KernelBuilder.Build();

            string instructions = File.ReadAllText(_configuration["Practibot:AgentInstructions"]??"");

            _agent = new()
            {
                Name = "Practibot",
                Description = "Practibot agent",
                Instructions = instructions,
                Kernel = _kernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()})
            };
            // Plugins Registration
            var conversationalSurveyPlugin = new ConversationalSurveyPlugin(configuration);
            _agent.Kernel.Plugins.AddFromObject(conversationalSurveyPlugin);
            var chatPlugin = new ChatPlugin(configuration);
            _agent.Kernel.Plugins.AddFromObject(chatPlugin);
            var surveyPlugin = new SurveyPlugin(configuration);
            _agent.Kernel.Plugins.AddFromObject(surveyPlugin);
            
        }

        public string InitAgent()
        {
            //ChatMessageContent response;
            string initMessage = File.ReadAllText(_configuration["Practibot:AgentInit"]??"");
            return initMessage ?? String.Empty;  
        }
        
        public async Task<ChatResponse> Chat(string message, ChatDeployment chat){
            string _message = string.Empty;
            string lastMessage = _chatService.GetChatContext(chat, 1);
            string chatContext = _chatService.GetChatContext(chat, 5);
            Question lastQuestion = chat.LastAskedQuestion;

            // matches the message to the questions in the survey
            KernelArguments matchArgs = new KernelArguments{["message"] = message, ["question"] = lastQuestion};
            Question? answeredQuestion = await _kernel.InvokeAsync<Question>("SurveyPlugin", "MatchMessageToQuestions", matchArgs);
            if(answeredQuestion != null && answeredQuestion.hasAnswer)
            {
                // Add the question to the chat history
                string metadata = $"{answeredQuestion.id}: {answeredQuestion.question} - {answeredQuestion.answer}";
                chat.Messages.Add(new SimpleChatMessage{Role=AuthorRole.System.Label, Content=metadata});
                chat.AnsweredSurvey.questions.Add(answeredQuestion);
                // Find and remove the question with matching ID instead of removing the object directly
                var questionToRemove = chat.Survey.questions.FirstOrDefault(q => q.id == answeredQuestion.id);
                if (questionToRemove != null)
                {
                    chat.Survey.questions.Remove(questionToRemove);
                }
            }
            
            // get the next unanswered question from the survey
            KernelArguments surveyArgs = new KernelArguments{["survey"] = chat.Survey, ["context"] = chatContext};
            Question? nextQuestion = await _kernel.InvokeAsync<Question>("SurveyPlugin", "GetNextUnansweredQuestion", surveyArgs);
            if (nextQuestion != null)
            {
                KernelArguments questionArgs = new KernelArguments{["question"] = nextQuestion};
                string questionText = await _kernel.InvokeAsync<string>("SurveyPlugin", "AskQuestion", questionArgs) ?? string.Empty;
                chat.LastAskedQuestion = nextQuestion;
                _message += StringUtilities.AppendBr(questionText);
            }

            // Returns response to the user
            ChatResponse response = new ChatResponse { Chat = chat, Message = _message, Success = true };
            return response;
        }

        public async Task<string> AcknowledgeAudioMessage(KernelArguments args, ChatDeployment chat)
        {
            string audioFormat = args["audioFormat"]?.ToString() ?? "unknown format";
            
            try
            {
                string acknowledgeMessage = await _kernel.InvokeAsync<string>("ChatPlugin", "AcknowledgeAudioMessage", new KernelArguments { ["audioFormat"] = audioFormat }) 
                    ?? "Thank you for your audio message.";
                return acknowledgeMessage;
            }
            catch (Exception ex)
            {
                return $"I received your audio message. {ex.Message}";
            }
        }
    }
}