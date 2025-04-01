namespace OLSOrca.Services.Interfaces
{
    /// <summary>
    /// Service for transcribing audio content to text
    /// </summary>
    public interface IAudioTranscriptionService
    {
        /// <summary>
        /// Transcribes audio data to text
        /// </summary>
        /// <param name="audioData">Base64 encoded audio data</param>
        /// <param name="audioFormat">Format of the audio (e.g., mp3, wav)</param>
        /// <param name="language">Optional language code (e.g., "en" for English)</param>
        /// <returns>Transcribed text</returns>
        Task<string> TranscribeAsync(string audioData, string audioFormat, string language = null);
    }
}
