using OLSOrca.Services;
using OLSOrca.Interfaces;
using OLSOrca.Plugins;
using Microsoft.SemanticKernel;
using OLSOrca.Services.Interfaces; // Add this namespace

//creates the API Builder
var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
//APIBuilder.Services.AddTransient<IAzureOpenAiPlugin, AzureOpenAiPlugin>();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<Kernel>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
//Registering the agent
builder.Services.AddTransient<IPractibotAgent, PractibotAgent>();
//REgistering the services
builder.Services.AddTransient<OrchestrationService>();
builder.Services.AddTransient<ICosmosService, CosmosService>();
builder.Services.AddTransient<IChatService, ChatService>(); 
builder.Services.AddTransient<ISurveyService, SurveyService>(); 
builder.Services.AddTransient<IAudioTranscriptionService, WhisperTranscriptionService>();
//Register Controllers
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin()
                         .AllowAnyMethod()
                         .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();
app.Run();
