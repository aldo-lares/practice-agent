using Microsoft.AspNetCore.Mvc;
using OLSOrca.Services;
using OLSOrca.Models;

namespace OLSOrca.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController : ControllerBase
    {
        private readonly OrchestrationService _orchestrationService;

        public ConversationController(OrchestrationService orchestrationService)
        {
            _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest chatRequest)
        {
            if (chatRequest == null)
                return BadRequest("Chat request cannot be null.");
                
            if (string.IsNullOrEmpty(chatRequest.SessionId)) 
                return BadRequest("Session ID is required.");
                
            if (string.IsNullOrEmpty(chatRequest.UserMessage)) 
                return BadRequest("User Message is required.");
            
            try
            {
                var response = await _orchestrationService.Chat(chatRequest.SessionId, chatRequest.UserMessage);
                
                if (!response.Success)
                    return StatusCode(500, response);
                    
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new APIResponse { Response = ex.Message, Success = false });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new APIResponse { Response = "An error occurred processing your request.", Success = false });
            }
        }

        [HttpPost("audio")]
        public async Task<IActionResult> Audio([FromBody] AudioRequest audioRequest)
        {
            if (audioRequest == null)
                return BadRequest("Audio request cannot be null.");
                
            if (string.IsNullOrEmpty(audioRequest.SessionId)) 
                return BadRequest("Session ID is required.");
                
            if (string.IsNullOrEmpty(audioRequest.AudioData)) 
                return BadRequest("Audio data is required.");
            
            try
            {
                var response = await _orchestrationService.ProcessAudio(audioRequest.SessionId, audioRequest.AudioData, audioRequest.AudioFormat);
                
                if (!response.Success)
                    return StatusCode(500, response);
                    
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new APIResponse { Response = ex.Message, Success = false });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new APIResponse { Response = "An error occurred processing your audio request.", Success = false });
            }
        }
    }
}