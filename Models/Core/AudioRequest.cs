namespace OLSOrca.Models
{
    public class AudioRequest
    {
        public string AudioData { get; set; } = string.Empty; // Base64 encoded audio data
        public string SessionId { get; set; } = string.Empty;
        public string AudioFormat { get; set; } = string.Empty; // Format of the audio (e.g., mp3, wav)
    }
}
