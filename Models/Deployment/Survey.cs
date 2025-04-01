using System.Text.Json.Serialization;
using System.Text;

namespace OLSOrca.Models
{
    public class Survey
    {
        [JsonPropertyName("questions")]
        public List<Question> questions { get; set; } = new List<Question>();

        public string GetSurveyAsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var question in questions)
            {
                sb.AppendLine($"id = {question.id}, question = \"{question.question}\"");
                if (question.choices != null && question.choices.Count > 0)
                {
                    sb.AppendLine($"choices = [{string.Join(", ", question.choices)}]");
                }
            }
            return sb.ToString();
        }
    }
}