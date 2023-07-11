namespace SIL.Machine.AspNetCore.Models;

public class TrainSegmentPair : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;
    public int Revision { get; set; } = 1;
    public string TranslationEngineRef { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string Target { get; set; } = default!;
    public bool SentenceStart { get; set; }
}
