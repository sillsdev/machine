using System.IO;

namespace SIL.Machine.Translation
{
	public interface ICommandSpec
	{
		void AddParameters(CommandBase command);
		bool Validate(TextWriter outWriter);
	}
}
