using System.Data;
using System.Net.Mail;
using ClosedXML.Excel;
using LeadDesk.Api.Data;
using LeadDesk.Api.DTOs;
using LeadDesk.Api.Models;
using Microsoft.AspNetCore.Http;

namespace LeadDesk.Api.Services;

public sealed class LeadService(ILeadRepository repository, IDbConnection connection) : ILeadService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public async Task<LeadPageResponse> SearchAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1.");
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        if (search?.Length > 200) throw new ArgumentOutOfRangeException(nameof(search), "Search text cannot exceed 200 characters.");
        var (items, totalCount) = await repository.SearchAsync(search, pageNumber, pageSize, cancellationToken);
        return new LeadPageResponse(items, pageNumber, pageSize, totalCount);
    }

    public Task<IReadOnlyList<Lead>> ExportAsync(string? search, CancellationToken cancellationToken) =>
        repository.GetAllAsync(search, cancellationToken);

    public async Task<ImportResult> ImportExcelAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) throw new InvalidDataException("The uploaded file is empty.");
        if (file.Length > MaxFileSizeBytes) throw new InvalidDataException("File size cannot exceed 10 MB.");
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Only .xlsx files are supported.");

        using var stream = file.OpenReadStream();
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidDataException("The uploaded file is not a valid .xlsx workbook.", ex);
        }
        using (workbook)
        {
            return await ImportWorkbookAsync(workbook, cancellationToken);
        }
    }

    private async Task<ImportResult> ImportWorkbookAsync(XLWorkbook workbook, CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The workbook does not contain a worksheet.");
        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidDataException("The worksheet is empty.");
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var normalizedHeader = NormalizeHeader(cell.GetString());
            if (normalizedHeader.Length == 0) continue;
            if (!headers.TryAdd(normalizedHeader, cell.Address.ColumnNumber))
                throw new InvalidDataException($"The worksheet contains a duplicate column header: '{cell.GetString()}'.");
        }

        var nameColumn = FindColumn(headers, "name");
        var mobileColumn = FindColumn(headers, "mobilenumber", "mobile", "mobileno", "phonenumber", "phone");
        var emailColumn = FindColumn(headers, "email", "emailaddress");
        if (nameColumn is null || mobileColumn is null)
            throw new InvalidDataException("Required columns are Name and Mobile Number. Email is optional.");

        var rows = new List<(int RowNumber, Lead? Lead, string? Error)>();
        foreach (var row in worksheet.RowsUsed().Where(row => row.RowNumber() > headerRow.RowNumber()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = row.Cell(nameColumn.Value).GetString().Trim();
            var mobile = row.Cell(mobileColumn.Value).GetString().Trim();
            var email = emailColumn is null ? null : row.Cell(emailColumn.Value).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(email)) continue;
            if (string.IsNullOrWhiteSpace(name))
            {
                rows.Add((row.RowNumber(), null, "Name is required."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(mobile))
            {
                rows.Add((row.RowNumber(), null, "Mobile Number is required."));
                continue;
            }
            if (name.Length > 150 || mobile.Length > 25 || (email?.Length ?? 0) > 254)
            {
                rows.Add((row.RowNumber(), null, "One or more values exceed the allowed length."));
                continue;
            }
            var normalizedMobile = NormalizeMobileNumber(mobile);
            if (normalizedMobile is null || normalizedMobile.Length == 0 || normalizedMobile.Length > 25)
            {
                rows.Add((row.RowNumber(), null, "Mobile Number contains unsupported characters."));
                continue;
            }
            mobile = normalizedMobile;
            if (!string.IsNullOrWhiteSpace(email) && !MailAddress.TryCreate(email, out _))
            {
                rows.Add((row.RowNumber(), null, "Email format is invalid."));
                continue;
            }
            rows.Add((row.RowNumber(), new Lead
            {
                Name = name,
                MobileNumber = mobile,
                Email = string.IsNullOrWhiteSpace(email) ? null : email.ToLowerInvariant(),
                CreatedAtUtc = DateTime.UtcNow
            }, null));
        }

        var importedCount = 0;
        var existingCount = 0;
        var invalidCount = 0;
        var errors = new List<string>();
        var seenMobiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (connection.State != ConnectionState.Open) connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var item in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.Lead is null)
                {
                    invalidCount++;
                    errors.Add($"Row {item.RowNumber}: {item.Error}");
                    continue;
                }

                var lead = item.Lead;
                // Evaluate both sets independently; short-circuiting here can miss a later email duplicate.
                var duplicateMobileInFile = !seenMobiles.Add(lead.MobileNumber);
                var duplicateEmailInFile = lead.Email is not null && !seenEmails.Add(lead.Email);
                if (duplicateMobileInFile || duplicateEmailInFile ||
                    await repository.ExistsAsync(lead.MobileNumber, lead.Email, transaction, cancellationToken))
                {
                    existingCount++;
                    errors.Add($"Row {item.RowNumber}: Duplicate mobile number or email; skipped.");
                    continue;
                }
                await repository.InsertAsync(lead, transaction, cancellationToken);
                importedCount++;
            }

            var summaryId = await repository.SaveImportSummaryAsync(DateTime.UtcNow,
                rows.Count, importedCount, existingCount, invalidCount, transaction, cancellationToken);
            transaction.Commit();
            return new ImportResult(summaryId, rows.Count, importedCount, existingCount, invalidCount, errors);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static int? FindColumn(IReadOnlyDictionary<string, int> headers, params string[] candidates)
    {
        var matchingColumns = candidates
            .Where(headers.ContainsKey)
            .Select(candidate => headers[candidate])
            .Distinct()
            .ToArray();

        if (matchingColumns.Length > 1)
            throw new InvalidDataException("The worksheet contains multiple columns for the same field. Keep only one matching column.");

        return matchingColumns.Length == 1 ? matchingColumns[0] : null;
    }

    private static string NormalizeHeader(string header) =>
        new string(header.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string? NormalizeMobileNumber(string mobileNumber)
    {
        var normalized = new string(mobileNumber
            .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '(' and not ')' and not '.')
            .ToArray());

        var digits = normalized.StartsWith('+') ? normalized[1..] : normalized;
        if (digits.Length == 0 || digits.Any(character => !char.IsDigit(character))) return null;

        return normalized.StartsWith('+') ? $"+{digits}" : digits;
    }
}
