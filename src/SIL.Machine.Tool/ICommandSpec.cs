using System.IO;

namespace SIL.Machine
{
	public interface ICommandSpec
	{
		void AddParameters(CommandBase command);
		bool Validate(TextWriter outWriter);
	}
}
