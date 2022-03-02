namespace SIL.Machine.WebApi.Models;

public class Lock : ICloneable<Lock>
{
	public Lock()
	{
	}

	public Lock(Lock @lock)
	{
		Id = @lock.Id;
		ExpiresAt = @lock.ExpiresAt;
		IsAcquired = @lock.IsAcquired;
	}

	public string Id { get; set; } = default!;
	public DateTime? ExpiresAt { get; set; }
	public bool IsAcquired { get; set; }

	public Lock Clone()
	{
		return new Lock(this);
	}
}
