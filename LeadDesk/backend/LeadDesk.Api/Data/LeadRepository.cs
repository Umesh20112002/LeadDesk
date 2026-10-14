using System.Data;
using Dapper;
using LeadDesk.Api.Models;

namespace LeadDesk.Api.Data;

public sealed class LeadRepository(IDbConnection connection) : ILeadRepository
{
    public async Task<(IReadOnlyList<Lead> Items, long TotalCount)> SearchAsync(
        string? search, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            "dbo.usp_Leads_Search",
            new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);
        var totalCount = await multi.ReadSingleAsync<long>();
        var leads = (await multi.ReadAsync<Lead>()).AsList();
        return (leads, totalCount);
    }

    public async Task<IReadOnlyList<Lead>> GetAllAsync(string? search, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, Name, MobileNumber, Email, CreatedAtUtc
            FROM dbo.Leads
            WHERE @Search IS NULL OR Name LIKE @Pattern OR MobileNumber LIKE @Pattern OR Email LIKE @Pattern
            ORDER BY CreatedAtUtc DESC, Id DESC;
            """;
        var command = new CommandDefinition(sql, new
        {
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Pattern = $"%{search?.Trim()}%"
        }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<Lead>(command)).AsList();
    }

    public async Task<bool> ExistsAsync(string mobileNumber, string? email, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Leads
                WHERE MobileNumber = @MobileNumber
                   OR (@Email IS NOT NULL AND Email = @Email)
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        var command = new CommandDefinition(sql, new { MobileNumber = mobileNumber, Email = email }, transaction, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task InsertAsync(Lead lead, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Leads (Name, MobileNumber, Email, CreatedAtUtc)
            VALUES (@Name, @MobileNumber, @Email, @CreatedAtUtc);
            """;
        var command = new CommandDefinition(sql, lead, transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task<long> SaveImportSummaryAsync(DateTime importedAtUtc, int totalRows, int importedCount,
        int existingCount, int invalidCount, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ImportSummary (ImportedAtUtc, TotalRows, ImportedCount, ExistingCount, InvalidCount)
            OUTPUT INSERTED.Id
            VALUES (@ImportedAtUtc, @TotalRows, @ImportedCount, @ExistingCount, @InvalidCount);
            """;
        var command = new CommandDefinition(sql, new { ImportedAtUtc = importedAtUtc, TotalRows = totalRows,
            ImportedCount = importedCount, ExistingCount = existingCount, InvalidCount = invalidCount },
            transaction, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<long>(command);
    }
}
