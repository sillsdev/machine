namespace SIL.Machine.WebApi
{
	public class DataFileDto : ResourceDto
	{
		public ResourceDto Engine { get; set; }
		public string Name { get; set; }
		public string Format { get; set; }
		public string DataType { get; set; }
	}
}
