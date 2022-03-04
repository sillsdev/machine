namespace SIL.Machine.WebApi.Models;

public class Lock : ICloneable<Lock>
{
	public Lock()
	{
	}

	public Lock(Lock lck)
	{
		Id = lck.Id;
		ExpiresAt = lck.ExpiresAt;
		HostId = lck.HostId;
	}

	public string Id { get; set; } = default!;
	public DateTime? ExpiresAt { get; set; }
	public string HostId { get; set; } = default!;

	public Lock Clone()
	{
		return new Lock(this);
	}
}
