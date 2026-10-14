using LeadDesk.Api.DTOs;
using LeadDesk.Api.Models;
using Microsoft.AspNetCore.Http;

namespace LeadDesk.Api.Services;

public interface ILeadService
{
    Task<LeadPageResponse> SearchAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lead>> ExportAsync(string? search, CancellationToken cancellationToken);
    Task<ImportResult> ImportExcelAsync(IFormFile file, CancellationToken cancellationToken);
}
