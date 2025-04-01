using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PracticeAgent.Models;
using PracticeAgent.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace PracticeAgent.Services
{
    public class SurveyService : ISurveyService
    {    
        public string _functionPath { get; set; }

        public SurveyService(IConfiguration configuration){
            _functionPath = Path.Combine(configuration["Practibot:SemanticPlugins"]??"","SurveyPlugin");
        }
        public Survey LoadSurveyFromFile()
        {
            try
            {
                // Construct the file path
                string filePath = Path.Combine(_functionPath, "OriginalSurvey.txt");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Survey file not found: {filePath}");
                }
                
                // Read the file content
                string fileContent = File.ReadAllText(filePath);
                
                // Parse into a Survey object
                Survey survey = new Survey();
                
                // Split by lines and process each line
                string[] lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    // Parse using regex to extract id, question and choices
                    var match = Regex.Match(line, @"id\s*=\s*(\d+),\s*question\s*=\s*""([^""]+)""(?:,\s*choices\s*=\s*\[(.*?)\])?");
                    if (match.Success)
                    {
                        int id = int.Parse(match.Groups[1].Value);
                        string questionText = match.Groups[2].Value;
                        string choicesText = match.Groups[3].Value;
                        
                        Question question = new Question
                        {
                            id = id,
                            question = questionText
                        };
                        
                        // Add choices if they exist (would need to extend Question class to support this)
                        if (!string.IsNullOrEmpty(choicesText))
                        {
                            question.choices = choicesText.Split(new[] { ',', '"' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(c => c.Trim())
                                .Where(c => !string.IsNullOrEmpty(c))
                                .ToList();
                        }
                        
                        survey.questions.Add(question);
                    }
                }
                
                return survey;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading survey from file: {ex.Message}");
                // Return an empty survey on error
                return new Survey();
            }
        }
    }
}