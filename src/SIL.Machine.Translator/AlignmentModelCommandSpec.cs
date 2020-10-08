using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Plugin;

namespace SIL.Machine.Translation
{
	public class AlignmentModelCommandSpec : ICommandSpec
	{
		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;
		private CommandOption _pluginOption;

		public string ModelType => _modelTypeOption.Value();
		public string ModelPath => _modelArgument.Value;
		public IWordAlignmentModelFactory ModelFactory { get; private set; }

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The word alignment model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				"The word alignment model type.\nTypes: \"hmm\" (default), \"ibm1\", \"ibm2\", \"pt\", \"smt\".",
				CommandOptionType.SingleValue);
			_pluginOption = command.Option("-mp|--model-plugin <PLUGIN_FILE>", "The model plugin file.",
				CommandOptionType.SingleValue);
		}

		public bool Validate(TextWriter outWriter)
		{
			if (_pluginOption.HasValue() && _pluginOption.Values.Any(p => !File.Exists(p)))
			{
				outWriter.WriteLine("A specified plugin file does not exist.");
				return false;
			}

			var pluginLoader = new PluginManager(_pluginOption.Values);
			var factories = pluginLoader.Create<IWordAlignmentModelFactory>().ToDictionary(f => f.ModelType);

			if (!ValidateAlignmentModelTypeOption(_modelTypeOption.Value(), factories.Keys))
			{
				outWriter.WriteLine("The specified word alignment model type is invalid.");
				return false;
			}

			if (factories.TryGetValue(ModelType, out IWordAlignmentModelFactory factory))
				ModelFactory = factory;

			return true;
		}


		private static bool ValidateAlignmentModelTypeOption(string value, IEnumerable<string> pluginTypes)
		{
			var validTypes = new HashSet<string> { "hmm", "ibm1", "ibm2", "pt", "smt" };
			validTypes.UnionWith(pluginTypes);
			return string.IsNullOrEmpty(value) || validTypes.Contains(value);

		}
	}
}
