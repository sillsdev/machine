namespace SIL.Machine.WebApi.Models
{
	public class Project
	{
		public Project(string id, bool isShared, string dir, Engine engine)
		{
			Id = id;
			IsShared = isShared;
			Directory = dir;
			Engine = engine;
		}

		public string Id { get; }
		public bool IsShared { get; }
		public string Directory { get; }
		public Engine Engine { get; }
	}
}
