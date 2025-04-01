using Microsoft.SemanticKernel.ChatCompletion;
using PracticeAgent.Models;

namespace PracticeAgent.Services.Interfaces
{
    public interface ISurveyService
    {
        public Survey LoadSurveyFromFile();
    }
}
