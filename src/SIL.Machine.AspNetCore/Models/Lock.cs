namespace SIL.Machine.AspNetCore.Models;

public class Lock
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;
    public DateTime? ExpiresAt { get; set; }
    public string HostId { get; set; } = default!;
}
