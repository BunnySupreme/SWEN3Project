using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paperless.Services;
using System.Security.Claims;

namespace Paperless.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private const int DefaultTake = 50;
    private const int MaxTake = 100;
    private readonly IDocumentService _svc;

    public DocumentsController(IDocumentService svc) => _svc = svc;

    // ─────────────────────────────────────────────
    // LIST
    // ─────────────────────────────────────────────
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? title,
        [FromQuery] int skip = 0,
        [FromQuery] int take = DefaultTake,
        CancellationToken ct = default)
    {
        if (skip < 0 || take < 1 || take > MaxTake)
            return BadRequest(new { message = $"skip >= 0, 1 <= take <= {MaxTake}" });

        var userId = GetUserId();
        var docs = await _svc.ListAsync(userId, title, skip, take, ct);

        return Ok(docs);
    }

    // ─────────────────────────────────────────────
    // GET
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var doc = await _svc.GetAsync(userId, id, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    // ─────────────────────────────────────────────
    // DOWNLOAD
    // ─────────────────────────────────────────────
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var memoryStream = await _svc.DownloadAsync(userId, id, ct);
        if (memoryStream is null)
            return NotFound();

        var fileName = $"{id}.pdf";

        return File(memoryStream, "application/pdf", fileName);
    }

    // ─────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────
    [HttpPost]
    [ProducesResponseType(typeof(DocumentReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] DocumentCreateDto dto,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        var created = await _svc.CreateAsync(userId, dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    // ─────────────────────────────────────────────
    // UPLOAD
    // ─────────────────────────────────────────────
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentReadDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? title,
        [FromForm] string? tags,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var allowedContentTypes = new[] { "application/pdf" };
        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            // 415 = Unsupported Media Type
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                $"Only PDF files are allowed. Received: {file.ContentType}");
        }

        // also test ending additonally
        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                $"Only .pdf files are allowed. Received: {extension}");
        }

        var normalizedTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileName(file.FileName)
            : title.Trim();

        var tagList = string.IsNullOrWhiteSpace(tags)
            ? Array.Empty<string>()
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var dto = new DocumentCreateDto(
            Title: normalizedTitle,
            Summary: string.Empty,
            Tags: tagList
        );

        var userId = GetUserId();
        var created = await _svc.UploadAsync(userId, file, dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] DocumentUpdateDto dto,
        CancellationToken ct = default)
    {
        if (id != dto.Id) return BadRequest(new { message = "Route id must match body id" });

        var userId = GetUserId();
        var ok = await _svc.UpdateAsync(userId, dto, ct);
        return ok ? NoContent() : NotFound();
    }

    // ─────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var ok = await _svc.DeleteAsync(userId, id, ct);
        return ok ? NoContent() : NotFound();
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user id claim.");

        return userId;
    }
}
