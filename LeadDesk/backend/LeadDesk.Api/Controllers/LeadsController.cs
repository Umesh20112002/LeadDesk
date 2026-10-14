using System.Globalization;
using ClosedXML.Excel;
using LeadDesk.Api.DTOs;
using LeadDesk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadDesk.Api.Controllers;

[ApiController]
[Route("api/leads")]
public sealed class LeadsController(ILeadService leadService, ILogger<LeadsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(LeadPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeads([FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await leadService.SearchAsync(search, pageNumber, pageSize, cancellationToken));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(new { message = "Please select an .xlsx file." });
        try
        {
            return Ok(await leadService.ImportExcelAsync(file, cancellationToken));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excel import failed for file {FileName}", Path.GetFileName(file.FileName));
            return Problem(title: "Import failed", detail: "The file could not be processed. Check the file format and application logs.", statusCode: 500);
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (search?.Length > 200)
                return BadRequest(new { message = "Search text cannot exceed 200 characters." });

            var leads = await leadService.ExportAsync(search, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Leads");
            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Mobile Number";
            sheet.Cell(1, 3).Value = "Email";
            sheet.Cell(1, 4).Value = "Created At (UTC)";
            var header = sheet.Range(1, 1, 1, 4);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightBlue;
            for (var i = 0; i < leads.Count; i++)
            {
                var lead = leads[i];
                var row = i + 2;
                sheet.Cell(row, 1).SetValue(lead.Name);
                sheet.Cell(row, 2).SetValue(lead.MobileNumber);
                sheet.Cell(row, 3).SetValue(lead.Email ?? string.Empty);
                // Keep contact data as text so Excel does not reinterpret phone numbers or user input.
                sheet.Cell(row, 4).Value = lead.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            sheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "leads.xlsx");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lead export failed");
            return Problem(title: "Export failed", detail: "Leads could not be exported. Please try again.", statusCode: 500);
        }
    }
}
