namespace SIL.Machine.WebApi.Models;

public class DataFile : IEntity<DataFile>
{
	public DataFile()
	{
	}

	public DataFile(DataFile dataFile)
	{
		Id = dataFile.Id;
		EngineRef = dataFile.EngineRef;
		Name = dataFile.Name;
		Format = dataFile.Format;
		DataType = dataFile.DataType;
		Filename = dataFile.Filename;
	}

	public string Id { get; set; } = default!;
	public string EngineRef { get; set; } = default!;
	public string Name { get; set; } = default!;
	public string Format { get; set; } = default!;
	public string DataType { get; set; } = default!;
	public string Filename { get; set; } = default!;

	public DataFile Clone()
	{
		return new DataFile(this);
	}
}
