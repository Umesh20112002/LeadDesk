using LeadDesk.Api.Models;

namespace LeadDesk.Api.DTOs;

public sealed record LeadPageResponse(
    IReadOnlyList<Lead> Items,
    int PageNumber,
    int PageSize,
    long TotalCount);
