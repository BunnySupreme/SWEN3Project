using Microsoft.AspNetCore.Mvc;
using paperless.Services;

namespace paperless.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessLogController : ControllerBase
    {
        private const int DefaultTake = 50;
        private const int MaxTake = 100;
        private readonly IXmlService _xmlSvc;

        public AccessLogController(IXmlService xmlSvc)
        {
            _xmlSvc = xmlSvc;
        }

        // ─────────────────────────────────────────────
        // LOG ACCESS
        // ─────────────────────────────────────────────
        [HttpPost("{id:guid}/doc-access")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DocAccess(
            Guid id,
            CancellationToken ct = default)
        {
            await _xmlSvc.UpdateAsync(id);
            return Ok();
        }
    }
}
