using Azunt.AttachmentManagement;
using Microsoft.AspNetCore.Mvc;

namespace Azunt.Web.Components.Pages.Attachments.Apis;

[ApiController]
[Route("api/attachments")]
public class AttachmentApiController : ControllerBase
{
    private readonly IAttachmentRepository _repository;

    public AttachmentApiController(IAttachmentRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<ArticleSet<AttachmentRecord, long>>> GetPaged(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string searchQuery = "",
        [FromQuery] string sortOrder = "",
        [FromQuery] long? employeeId = null,
        [FromQuery] long? vendorId = null,
        [FromQuery] long? investigationId = null,
        [FromQuery] bool activeOnly = false)
    {
        return Ok(await _repository.GetPagedAsync(new AttachmentFilterOptions
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            SearchQuery = searchQuery,
            SortOrder = sortOrder,
            EmployeeId = employeeId,
            VendorId = vendorId,
            InvestigationId = investigationId,
            ActiveOnly = activeOnly
        }));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AttachmentRecord>> GetById(long id)
    {
        var model = await _repository.GetByIdAsync(id);
        return model is null ? NotFound() : Ok(model);
    }

    [HttpGet("investigation/{investigationId:long}")]
    public async Task<ActionResult<IEnumerable<AttachmentRecord>>> GetByInvestigation(long investigationId)
    {
        return Ok(await _repository.GetByInvestigationIdAsync(investigationId));
    }

    [HttpPost]
    public async Task<ActionResult<AttachmentRecord>> Create(AttachmentRecord model)
    {
        var created = await _repository.AddAsync(model);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, AttachmentRecord model)
    {
        if (id != model.Id)
        {
            return BadRequest("Route ID and model ID do not match.");
        }

        return await _repository.UpdateAsync(model) ? NoContent() : NotFound();
    }

    [HttpPatch("{id:long}/metadata")]
    public async Task<IActionResult> UpdateMetadata(long id, AttachmentMetadataRequest request)
    {
        return await _repository.UpdateMetadataAsync(
            id,
            request.InvestigationId,
            request.Category,
            request.Notes,
            request.ModifiedBy)
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        return await _repository.DeleteAsync(id) ? NoContent() : NotFound();
    }
}

public sealed class AttachmentMetadataRequest
{
    public long? InvestigationId { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public string? ModifiedBy { get; set; }
}
