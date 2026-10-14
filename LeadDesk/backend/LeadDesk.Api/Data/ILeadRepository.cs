using System.Data;
using LeadDesk.Api.Models;

namespace LeadDesk.Api.Data;

public interface ILeadRepository
{
    Task<(IReadOnlyList<Lead> Items, long TotalCount)> SearchAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lead>> GetAllAsync(string? search, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string mobileNumber, string? email, IDbTransaction transaction, CancellationToken cancellationToken);
    Task InsertAsync(Lead lead, IDbTransaction transaction, CancellationToken cancellationToken);
    Task<long> SaveImportSummaryAsync(DateTime importedAtUtc, int totalRows, int importedCount, int existingCount, int invalidCount, IDbTransaction transaction, CancellationToken cancellationToken);
}
