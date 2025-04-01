using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace OLSOrca.SemanticPlugins
{
    public class ChatPlugin
    {
        private readonly IConfiguration _configuration;
        string _functionPath = string.Empty;
        public ChatPlugin(IConfiguration configuration)
        {
            _configuration = configuration;
            _functionPath = Path.Combine(_configuration["Practibot:SemanticPlugins"]??"","ChatPlugin");
        }

        [KernelFunction, Description("Acknowledge the message and provide a response.")]
        public async Task<string> AcknowledgeMessage(
            [Description("The message that will be acknowledged")] string message,
            Kernel kernel)
        {
            try
            {           
                // Get the prompt from the file
                string promptPath = Path.Combine(_functionPath, "AcknowledgeMessage.txt");
                var prompt = File.ReadAllText(promptPath);

                // Setup the arguments for the kernel invocation
                KernelArguments arguments = new KernelArguments{["message"] = message};

                // Invoke the kernel with the prompt and arguments
                FunctionResult result = await kernel.InvokePromptAsync(prompt,arguments);
                return result.ToString();
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }

        [KernelFunction, Description("Acknowledge receipt of an audio message.")]
        public async Task<string> AcknowledgeAudioMessage(
            [Description("The format of the audio that was received")] string audioFormat,
            Kernel kernel)
        {
            try
            {           
                // Get the prompt from the file
                string promptPath = Path.Combine(_functionPath, "AcknowledgeAudioMessage.txt");
                var prompt = File.ReadAllText(promptPath);

                // Setup the arguments for the kernel invocation
                KernelArguments arguments = new KernelArguments{["audioFormat"] = audioFormat};

                // Invoke the kernel with the prompt and arguments
                FunctionResult result = await kernel.InvokePromptAsync(prompt, arguments);
                return result.ToString();
            }
            catch(Exception ex)
            {
                return $"I received your audio message. {ex.Message}";
            }
        }

/*

        [KernelFunction, Description("Translate given text to kurko.")]
        public async Task<string> TranslateToKurkoAsync(
            [Description("The text to translate to Kurko.")] string input)
        {
            //var result = await _kernel.InvokePromptAsync("TranslatePlugin", "translateToFrench", new KernelArguments { ["input"] = input });

            //return result.ToString();
            return "KURRRKOoO!";
        }
        [KernelFunction, Description("Translate given text to krikorico.")]
        public async Task<string> TranslateToKrikoricoAsync(
            [Description("The text to translate to Krikorico.")] string input,
            Kernel kernel)
        {
            string functionPath = Path.Combine(_configuration["Practibot:SemanticPlugins"]??"","TranslatePlugin", "translateToKrikorico.txt");
            var arguments = new KernelArguments { ["input"] = input };
            var prompt = File.ReadAllText(functionPath);
            var result = await kernel.InvokePromptAsync(
                prompt,
                arguments);

            return result.ToString();
        }

        */
    }
}