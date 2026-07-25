using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Azunt.AttachmentManagement;

public static class AttachmentExcelExporter
{
    public static byte[] ExportToExcel(
        IEnumerable<AttachmentRecord> attachments,
        string worksheetName = "Attachments")
    {
        ArgumentNullException.ThrowIfNull(attachments);

        using var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = string.IsNullOrWhiteSpace(worksheetName) ? "Attachments" : worksheetName
            });

            sheetData.Append(CreateRow(
                "ID", "Active", "DateCreated", "CreatedAt", "CreatedBy",
                "EmployeeID", "VendorID", "InvestigationID", "FileName",
                "Discriminator", "Category", "Notes"));

            foreach (var item in attachments)
            {
                sheetData.Append(CreateRow(
                    item.Id.ToString(),
                    item.Active?.ToString() ?? string.Empty,
                    FormatDate(item.DateCreated),
                    FormatDate(item.CreatedAt),
                    item.CreatedBy ?? string.Empty,
                    item.EmployeeId?.ToString() ?? string.Empty,
                    item.VendorId?.ToString() ?? string.Empty,
                    item.InvestigationId?.ToString() ?? string.Empty,
                    item.FileName ?? string.Empty,
                    item.Discriminator ?? string.Empty,
                    item.Category ?? string.Empty,
                    item.Notes ?? string.Empty));
            }

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static string FormatDate(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") ?? string.Empty;

    private static Row CreateRow(params string[] values)
    {
        var row = new Row();
        foreach (var value in values)
        {
            row.Append(new Cell
            {
                DataType = CellValues.InlineString,
                InlineString = new InlineString(
                    new DocumentFormat.OpenXml.Spreadsheet.Text(value ?? string.Empty))
            });
        }
        return row;
    }
}
