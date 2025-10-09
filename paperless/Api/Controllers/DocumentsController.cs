using Microsoft.AspNetCore.Mvc;
using Paperless.Api.Contracts;

namespace Paperless.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
	[HttpGet]
	public ActionResult<IEnumerable<DocumentReadDto>> List([FromQuery] int skip = 0, [FromQuery] int take = 50) => Ok(Array.Empty<DocumentReadDto>());

	[HttpGet("{id:guid}")]
	public ActionResult<DocumentReadDto> Get(Guid id) => NotFound();

	[HttpPost]
	public ActionResult<DocumentReadDto> Create([FromBody] DocumentCreateDto dto)
		=> CreatedAtAction(nameof(Get), new { id = Guid.NewGuid() }, // temp
			new DocumentReadDto(Guid.NewGuid(), dto.FileName, dto.ContentType ?? "application/pdf", DateTimeOffset.UtcNow, dto.Summary, dto.Tags));

	[HttpPut("{id:guid}")]
	public IActionResult Update(Guid id, [FromBody] DocumentUpdateDto dto)
		=> id != dto.Id ? BadRequest() : NoContent();

	[HttpDelete("{id:guid}")]
	public IActionResult Delete(Guid id) => NoContent();
}
