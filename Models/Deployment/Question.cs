using System.Text.Json.Serialization;

namespace PracticeAgent.Models
{
    public class Question
    {
        [JsonPropertyName("id")]
        public int id { get; set; } = 0;

        [JsonPropertyName("question")]
        public string question { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public List<string> choices { get; set; } = new List<string>();

        [JsonPropertyName("answer")]
        public string answer { get; set; } = string.Empty;

        [JsonPropertyName("comment")]
        public string comment { get; set; } = string.Empty;

        [JsonPropertyName("hasAnswer")]
        public bool hasAnswer { get; set; } = false;

        public string ToString()
        {
            return $"id={id}, question=\"{question}\", choices=[{string.Join(", ", choices)}]";
        }
    }
}