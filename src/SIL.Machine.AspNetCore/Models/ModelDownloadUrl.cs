namespace SIL.Machine.AspNetCore.Models;

public class ModelDownloadUrl
{
    public string Url { get; set; } = default!;
    public int ModelRevision { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}
