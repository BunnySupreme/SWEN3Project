using Microsoft.AspNetCore.Mvc;
using Paperless.Services;

namespace Paperless.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private const int DefaultTake = 50;
    private const int MaxTake = 100;
    private readonly IDocumentService _documentSvc;
    private readonly IElasticService _elasticSvc;

    public DocumentsController(IDocumentService DocumentSvc, IElasticService elasticSvc)
    {
        _documentSvc = DocumentSvc;
        _elasticSvc = elasticSvc;
    }

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

        var docs = await _documentSvc.ListAsync(title, skip, take, ct);
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
        var doc = await _documentSvc.GetAsync(id, ct);
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
        var memoryStream = await _documentSvc.DownloadAsync(id, ct);
        if (memoryStream is null)
            return NotFound();

        var fileName = $"{id}.pdf";

        return File(memoryStream, "application/pdf", fileName);
    }

    // ─────────────────────────────────────────────
    // SEARCH
    // ─────────────────────────────────────────────
    [HttpPost("search")] // POST to allow complex search criteria in body and avoid URL length limits
    [ProducesResponseType(typeof(IEnumerable<DocumentReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromBody] string searchTerm, CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest(new { message = "Search term cannot be empty" });
        }

        var results = await _elasticSvc.SearchAsync(searchTerm, ct);

        return Ok(results);
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
        var created = await _documentSvc.CreateAsync(dto, ct);
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

        var created = await _documentSvc.UploadAsync(file, dto, ct);
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

        var ok = await _documentSvc.UpdateAsync(dto, ct);
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
        var ok = await _documentSvc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
