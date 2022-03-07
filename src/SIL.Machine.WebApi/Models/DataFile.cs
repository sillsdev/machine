namespace SIL.Machine.WebApi.Models;

public class DataFile : IEntity
{
	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 1;
	public string EngineRef { get; set; } = default!;
	public DataType DataType { get; set; } = default!;
	public string Name { get; set; } = default!;
	public FileFormat Format { get; set; } = default!;
	public CorpusType? CorpusType { get; set; } = default;
	public string? CorpusKey { get; set; } = default;
	public string Filename { get; set; } = default!;
}
