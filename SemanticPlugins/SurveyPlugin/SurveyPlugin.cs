using System.ComponentModel;
using Microsoft.SemanticKernel;
using PracticeAgent.Models;
using System.Text.RegularExpressions;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;
using NJsonSchema;

namespace PracticeAgent.SemanticPlugins
{
    public class SurveyPlugin
    {
        private readonly IConfiguration _configuration;
        string _functionPath = string.Empty;
        public SurveyPlugin(IConfiguration configuration)
        {
            _configuration = configuration;
            _functionPath = Path.Combine(_configuration["Practibot:SemanticPlugins"]??"","SurveyPlugin");
        }

       
        [KernelFunction, Description("Get the next unanswered question from the survey.")]
        public Question GetNextUnansweredQuestion(
            [Description("The survey object.")] Survey survey,
            Kernel kernel)
        {
            // Find the first unanswered question
            Question nextQuestion = survey.questions.FirstOrDefault(q => !q.hasAnswer);
            return nextQuestion;
        }

        [KernelFunction, Description("Check if the input text might answer any of the questions, also considers the previous messages as context.")]
        public async Task<Question> MatchMessageToQuestions(
            [Description("The message that might answer a question.")] string message,
            [Description("The question .")] Question question,
            Kernel kernel)
        {  
            try
            {
                
                // Get the prompt from the file
                string promptPath = Path.Combine(_functionPath, "MatchMessageToQuestions.txt");
                var prompt = File.ReadAllText(promptPath);
                var structuredOutput = JsonSchema.FromType<MatchResult>().ToJson();

                KernelArguments arguments = new KernelArguments
                {
                    ["input"] = message,
                    ["question"] = question.ToString(),
                    ["structuredOutput"] = structuredOutput
                };

                var result = await kernel.InvokePromptAsync(
                    prompt,
                    arguments);
                
                Console.WriteLine($"Result: {result.ToString()}");
                
                // Deserialize the structured output
                var matchResult = JsonConvert.DeserializeObject<MatchResult>(result.ToString());
                
                if (matchResult != null && matchResult.Success)
                {
                    // If successful, update the question with the answer
                    question.answer = matchResult.Response;
                    question.hasAnswer = true;
                    question.comment = $"{message}. ";
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            return question;
            
        }
    
        [KernelFunction, Description("Take a question and retrieves it in a conversational context.")]
        public async Task<string> AskQuestion(
            [Description("The question to retrieve.")] Question question,
            Kernel kernel)
        {  
            var response = string.Empty;
            try
            {
                
                // Get the prompt from the file
                string promptPath = Path.Combine(_functionPath, "AskQuestion.txt");
                var prompt = File.ReadAllText(promptPath);

                KernelArguments arguments = new KernelArguments
                {
                    ["question"] = question.ToString()
                };

                var result = await kernel.InvokePromptAsync(
                    prompt,
                    arguments);
                
                response = result.ToString();
                //match = JsonConvert.DeserializeObject<MatchMessageToQuestions>(result.ToString());
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            return response;
            
        }

        // Class to deserialize the structured output
        private class MatchResult
        {
            public string Response { get; set; }
            public bool Success { get; set; }
        }
    }
}