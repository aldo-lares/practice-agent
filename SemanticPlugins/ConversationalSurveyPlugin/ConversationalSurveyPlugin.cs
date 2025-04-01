using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Layouts;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using OLSOrca.Models;
using OpenAI.Chat;

namespace OLSOrca.SemanticPlugins
{
    public class ConversationalSurveyPlugin
    {
        private readonly IConfiguration _configuration;
        string _functionPath = string.Empty;
        public ConversationalSurveyPlugin(IConfiguration configuration)
        {
            _configuration = configuration;
            _functionPath = Path.Combine(_configuration["Practibot:SemanticPlugins"]??"","ConversationalSurveyPlugin");
        }

        [KernelFunction, Description("Identify if the answer might be related to any of the conversational questions.")]
        public async Task<MatchMessageToQuestions> MatchMessageToQuestions(
            [Description("The message that will match a questions.")] string input,
            Kernel kernel)
        {
            MatchMessageToQuestions match = new MatchMessageToQuestions();
            try
            {
                // Get the prompt from the file
                string questionsPath = Path.Combine(_functionPath, "ConversationalQuestions.txt");
                string questions = File.ReadAllText(questionsPath);
                        
                // Get the prompt from the file
                string promptPath = Path.Combine(_functionPath, "MatchMessageToQuestions.txt");
                var prompt = File.ReadAllText(promptPath);

                var schema = JsonSchema.FromType<Survey>();
                string structuredOutput = schema.ToString();

                ChatResponseFormat chatResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName:"questions",
                    jsonSchema: BinaryData.FromString(structuredOutput),
                    jsonSchemaIsStrict: true
                );

                KernelArguments arguments = new KernelArguments
                {
                    ["input"] = input,
                    ["questions"] = questions,
                    ["chatResponseFormat"] = chatResponseFormat
                };

                var result = await kernel.InvokePromptAsync(
                    prompt,
                    arguments);

                var json = result.ToString().Replace("```json", "").Replace("```", "");
                var token = JsonConvert.DeserializeObject<JToken>(json);
                // If the token is a JObject and contains the "questions" property, use that array.
                if (token is JObject obj && obj.ContainsKey("questions"))
                {
                    token = obj["questions"];
                }
                var questionList = token as JArray;
                if (questionList == null)
                {
                    throw new Exception("Expected a JSON array for questions.");
                }
                
                Survey survey = new Survey();
                foreach (var question in questionList)
                {
                    Question q = new Question
                    {
                        id = (int)(question["id"] ?? -1),
                        question = question["question"]?.ToString() ?? String.Empty,
                    };
                    survey.questions.Add(q);
                }
                
                match.survey = survey;
                match.message = json;
                match.success = true;

                return match;
            }
            catch(Exception ex)
            {
                
                match.message = ex.Message;
                return match;
            }
        }
    }
}