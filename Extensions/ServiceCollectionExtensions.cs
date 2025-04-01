using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OLSOrca.Services;
using OLSOrca.Services.Interfaces;

namespace OLSOrca.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTranscriptionServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure HTTP client
            services.AddHttpClient("OpenAI", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            
            // Register the transcription service
            services.AddSingleton<IAudioTranscriptionService, WhisperTranscriptionService>();
            
            return services;
        }
    }
}
