using Microsoft.SemanticKernel.ChatCompletion;
using OLSOrca.Models;

namespace OLSOrca.Services.Interfaces
{
    public interface ISurveyService
    {
        public Survey LoadSurveyFromFile();
    }
}
