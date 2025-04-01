using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace OLSOrca.SemanticPlugins
{
    public class TranslatePlugin
    {
        private readonly IConfiguration _configuration;
        public TranslatePlugin(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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
    }
}