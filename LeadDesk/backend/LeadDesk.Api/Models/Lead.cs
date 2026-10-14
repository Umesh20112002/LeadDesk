namespace LeadDesk.Api.Models;

public sealed class Lead
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
