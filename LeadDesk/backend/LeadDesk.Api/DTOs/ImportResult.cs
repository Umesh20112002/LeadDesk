namespace LeadDesk.Api.DTOs;

public sealed record ImportResult(
    long ImportSummaryId,
    int TotalRows,
    int ImportedCount,
    int ExistingCount,
    int InvalidCount,
    IReadOnlyList<string> Errors);
