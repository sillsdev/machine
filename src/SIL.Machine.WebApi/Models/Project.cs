namespace SIL.Machine.WebApi.Models
{
	public class Project
	{
		public Project(string id, bool isShared, Engine engine)
		{
			Id = id;
			IsShared = isShared;
			Engine = engine;
		}

		public string Id { get; }
		public bool IsShared { get; }
		public Engine Engine { get; }
	}
}
