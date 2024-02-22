namespace SIL.Machine.AspNetCore.Models;

public record ModelDownloadUrl
{
    public required string Url { get; init; }
    public required int ModelRevision { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
