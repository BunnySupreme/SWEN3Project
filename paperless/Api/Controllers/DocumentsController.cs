using Microsoft.AspNetCore.Mvc;
using Paperless.Api.Contracts;
using Paperless.Services;

namespace Paperless.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
	private const int DefaultTake = 50;
	private const int MaxTake = 100;
	private readonly IDocumentService _svc;

	public DocumentsController(IDocumentService svc) => _svc = svc;

	/// <summary>List documents with optional title filter and pagination.</summary>
	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<DocumentReadDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<IEnumerable<DocumentReadDto>>> List(
		[FromQuery] string? title,
		[FromQuery] int skip = 0,
		[FromQuery] int take = DefaultTake,
		CancellationToken ct = default)
	{
		if (skip < 0 || take < 1 || take > MaxTake)
			return BadRequest(new { message = $"skip >= 0, 1 <= take <= {MaxTake}" });

		var docs = await _svc.ListAsync(title, skip, take, ct);
		return Ok(docs);
	}

	/// <summary>Get a single document by id.</summary>
	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(DocumentReadDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<DocumentReadDto>> Get(Guid id, CancellationToken ct = default)
	{
		var doc = await _svc.GetAsync(id, ct);
		return doc is null ? NotFound() : Ok(doc);
	}

	/// <summary>Create a new document.</summary>
	[HttpPost]
	[ProducesResponseType(typeof(DocumentReadDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<DocumentReadDto>> Create(
		[FromBody] DocumentCreateDto dto,
		CancellationToken ct = default)
	{
		var created = await _svc.CreateAsync(dto, ct);
		return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
	}

	/// <summary>Update an existing document.</summary>
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

		var ok = await _svc.UpdateAsync(dto, ct);
		return ok ? NoContent() : NotFound();
	}

	/// <summary>Delete a document by id.</summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
	{
		var ok = await _svc.DeleteAsync(id, ct);
		return ok ? NoContent() : NotFound();
	}
}
