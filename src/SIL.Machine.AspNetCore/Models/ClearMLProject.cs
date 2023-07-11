namespace SIL.Machine.AspNetCore.Models;

public class ClearMLProject
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;
}
