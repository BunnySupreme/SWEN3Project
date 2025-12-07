using Microsoft.AspNetCore.Mvc;
using Paperless.GeminiSummarizer.Services;

namespace Paperless.GeminiSummarizer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly GeminiService _service;

        public GeminiController(GeminiService service)
        {
            _service = service;
        }

        [HttpPost("summarize")]
        public async Task<ActionResult<SummarizeResponse>> Summarize([FromBody] SummarizeRequest req)
        {
            var summary = await _service.SummarizeTextAsync(req.Text, HttpContext.RequestAborted);
            return Ok(new SummarizeResponse(summary));
        }

        [HttpGet("ping")]
        public IActionResult Ping() => Ok("pong");
    }

    public record SummarizeRequest(string Text);
    public record SummarizeResponse(string Summary);
}
