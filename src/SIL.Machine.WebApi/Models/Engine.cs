using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class Engine : IEntity<Engine>
	{
		public Engine()
		{
			Projects = new HashSet<string>();
		}

		public Engine(Engine engine)
		{
			Id = engine.Id;
			Revision = engine.Revision;
			SourceLanguageTag = engine.SourceLanguageTag;
			TargetLanguageTag = engine.TargetLanguageTag;
			IsShared = engine.IsShared;
			Projects = new HashSet<string>(engine.Projects);
		}

		public string Id { get; set; }
		public long Revision { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public bool IsShared { get; set; }
		public ISet<string> Projects { get; }

		public Engine Clone()
		{
			return new Engine(this);
		}
	}
}
