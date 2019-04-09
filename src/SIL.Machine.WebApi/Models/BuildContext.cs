namespace SIL.Machine.WebApi.Models
{
	public class BuildContext
	{
		public BuildContext(Engine engine, Build build)
		{
			Engine = engine;
			Build = build;
		}

		public Engine Engine { get; }
		public Build Build { get; }
	}
}
