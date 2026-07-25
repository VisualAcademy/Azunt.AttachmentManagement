using Azunt.AttachmentManagement;
using Microsoft.AspNetCore.Mvc;

namespace Azunt.Web.Components.Pages.Attachments.Apis;

[ApiController]
[Route("api/attachment-export")]
public class AttachmentExportController : ControllerBase
{
    private readonly IAttachmentRepository _repository;

    public AttachmentExportController(IAttachmentRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel()
    {
        var items = await _repository.GetAllAsync();
        var bytes = AttachmentExcelExporter.ExportToExcel(items);
        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_Attachments.xlsx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
