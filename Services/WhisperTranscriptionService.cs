using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OLSOrca.Services.Interfaces;

namespace OLSOrca.Services
{
    /// <summary>
    /// Implementation of IAudioTranscriptionService using OpenAI's Whisper model
    /// </summary>
    public class WhisperTranscriptionService : IAudioTranscriptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhisperTranscriptionService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;
        private readonly string _defaultLanguage;

        public WhisperTranscriptionService(
            IConfiguration configuration,
            ILogger<WhisperTranscriptionService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("OpenAI");
            
            _apiKey = _configuration["OpenAI:ApiKey"] 
                ?? throw new ArgumentNullException("OpenAI:ApiKey configuration is missing");
            
            _apiEndpoint = _configuration["OpenAI:TranscriptionEndpoint"] 
                ?? "https://api.openai.com/v1/audio/transcriptions";
                
            _defaultLanguage = _configuration["OpenAI:DefaultLanguage"];
            if (!string.IsNullOrEmpty(_defaultLanguage))
            {
                _logger.LogInformation("Default transcription language set to: {Language}", _defaultLanguage);
            }
        }

        /// <summary>
        /// Transcribes audio data to text
        /// </summary>
        /// <param name="audioData">Base64 encoded audio data</param>
        /// <param name="audioFormat">Format of the audio (e.g., mp3, wav)</param>
        /// <param name="language">Optional language code to override the default from app settings</param>
        /// <returns>Transcribed text that will be processed as a user chat message</returns>
        public async Task<string> TranscribeAsync(string audioData, string audioFormat, string language = null)
        {
            // Use the provided language or fall back to the default from app settings
            string transcriptionLanguage = language ?? _defaultLanguage;
            
            _logger.LogInformation("Transcription request: Format={Format}, DataLength={Length}, Language={Language}", 
                audioFormat, audioData?.Length ?? 0, string.IsNullOrEmpty(transcriptionLanguage) ? "auto" : transcriptionLanguage);
                
            // For debugging, you could save the audio to a temp file
            if (_configuration.GetValue<bool>("Debug:SaveAudioFiles"))
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"whisper_debug_{Guid.NewGuid()}.{audioFormat}");
                
                // Extract base64 data using the helper method
                string base64Data = ExtractBase64FromDataUrl(audioData);
                
                await File.WriteAllBytesAsync(tempPath, Convert.FromBase64String(base64Data));
                _logger.LogInformation("Saved debug audio file to {Path}", tempPath);
            }

            try
            {
                _logger.LogInformation("Starting transcription for audio in {Format} format", audioFormat);
                
                // Extract base64 data using the helper method
                audioData = ExtractBase64FromDataUrl(audioData);
                
                // Convert base64 audio data to bytes
                byte[] audioBytes = Convert.FromBase64String(audioData);
                
                // Set up the request to OpenAI's Whisper API
                using var content = new MultipartFormDataContent();
                
                // Add the audio file
                var audioContent = new ByteArrayContent(audioBytes);
                string fileName = $"audio.{GetFileExtension(audioFormat)}";
                content.Add(audioContent, "file", fileName);
                
                // Set the model to use
                content.Add(new StringContent("whisper-1"), "model");
                
                // Add language if specified (from parameter or default)
                if (!string.IsNullOrEmpty(transcriptionLanguage))
                {
                    content.Add(new StringContent(transcriptionLanguage), "language");
                    _logger.LogInformation("Setting transcription language to: {Language}", transcriptionLanguage);
                }
                
                // Set up the request
                using var request = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint);
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Content = content;
                
                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                // Process the response
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<WhisperResponse>(jsonResponse, options);
                
                _logger.LogInformation("Transcription completed successfully. Text will be processed as a chat message.");
                
                return result?.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audio transcription");
                throw new ApplicationException("Failed to transcribe audio", ex);
            }
        }
        
        private string ExtractBase64FromDataUrl(string dataUrl)
        {
            if (dataUrl != null && dataUrl.StartsWith("data:"))
            {
                int base64Index = dataUrl.IndexOf("base64,");
                if (base64Index > 0)
                {
                    return dataUrl.Substring(base64Index + 7); // 7 is the length of "base64,"
                }
            }
            return dataUrl;
        }
        
        private string GetFileExtension(string audioFormat)
        {
            // Map audio format to file extension
            return audioFormat?.ToLower() switch
            {
                "mp3" => "mp3",
                "wav" => "wav",
                "m4a" => "m4a",
                "mp4" => "mp4",
                "mpeg" => "mpeg",
                "mpga" => "mpga",
                "webm" => "webm",
                "ogg" => "ogg",
                _ => "mp3"  // Default to mp3 if unknown
            };
        }
        
        private class WhisperResponse
        {
            public string Text { get; set; }
        }
    }
}
