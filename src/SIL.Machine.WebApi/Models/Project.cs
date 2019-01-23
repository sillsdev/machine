namespace SIL.Machine.WebApi.Models
{
	public class Project : IEntity<Project>
	{
		public Project()
		{
		}

		public Project(Project project)
		{
			Id = project.Id;
			Revision = project.Revision;
			SourceLanguageTag = project.SourceLanguageTag;
			TargetLanguageTag = project.TargetLanguageTag;
			SourceSegmentType = project.SourceSegmentType;
			TargetSegmentType = project.TargetSegmentType;
			IsShared = project.IsShared;
			EngineRef = project.EngineRef;
		}

		public string Id { get; set; }
		public int Revision { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public string SourceSegmentType { get; set; }
		public string TargetSegmentType { get; set; }
		public bool IsShared { get; set; }
		public string EngineRef { get; set; }

		public Project Clone()
		{
			return new Project(this);
		}
	}
}
