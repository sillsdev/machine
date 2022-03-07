namespace SIL.Machine.WebApi.Models;

public class Lock
{
	public string Id { get; set; } = default!;
	public DateTime? ExpiresAt { get; set; }
	public string HostId { get; set; } = default!;
}
