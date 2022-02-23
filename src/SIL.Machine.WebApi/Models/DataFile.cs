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

	public string Id { get; set; }
	public string EngineRef { get; set; }
	public string Name { get; set; }
	public string Format { get; set; }
	public string DataType { get; set; }
	public string Filename { get; set; }

	public DataFile Clone()
	{
		return new DataFile(this);
	}
}
