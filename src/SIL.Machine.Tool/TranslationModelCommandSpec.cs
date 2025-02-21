using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Plugin;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine;

public class TranslationModelCommandSpec : ICommandSpec
{
    private CommandArgument _modelArgument;
    private CommandOption _modelTypeOption;
    private CommandOption _pluginOption;

    private string _modelConfigFileName;
    private ITranslationModelFactory _modelFactory;

    public void AddParameters(CommandBase command)
    {
        _modelArgument = command.Argument("MODEL_PATH", "The translation model.").IsRequired();
        _modelTypeOption = command.Option(
            "-mt|--model-type <MODEL_TYPE>",
            $"The word alignment model type.\nTypes: \"{ToolHelpers.Hmm}\" (default), \"{ToolHelpers.Ibm1}\", \"{ToolHelpers.Ibm2}\", \"{ToolHelpers.FastAlign}\".",
            CommandOptionType.SingleValue
        );
        _pluginOption = command.Option(
            "-mp|--model-plugin <PLUGIN_FILE>",
            "The model plugin file.",
            CommandOptionType.SingleValue
        );
    }

    public bool Validate(TextWriter outWriter)
    {
        if (_pluginOption.HasValue() && _pluginOption.Values.Any(p => !File.Exists(p)))
        {
            outWriter.WriteLine("A specified plugin file does not exist.");
            return false;
        }

        var pluginLoader = new PluginManager(_pluginOption.Values);
        var factories = pluginLoader.Create<ITranslationModelFactory>().ToDictionary(f => f.ModelType);

        if (!ToolHelpers.ValidateTranslationModelTypeOption(_modelTypeOption.Value()))
        {
            outWriter.WriteLine("The specified model type is invalid.");
            return false;
        }

        _modelConfigFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);

        if (
            _modelTypeOption.HasValue()
            && factories.TryGetValue(_modelTypeOption.Value(), out ITranslationModelFactory factory)
        )
        {
            _modelFactory = factory;
        }

        return true;
    }

    public ITranslationModel CreateModel()
    {
        if (_modelFactory != null)
            return _modelFactory.CreateModel(_modelArgument.Value);

        ThotWordAlignmentModelType wordAlignmentModelType =
            ThotWordAlignmentModelTypeHelpers.GetThotWordAlignmentModelType(_modelTypeOption.Value());

        return new ThotSmtModel(wordAlignmentModelType, _modelConfigFileName);
    }

    public ITrainer CreateTrainer(IParallelTextCorpus corpus, int maxSize)
    {
        if (_modelFactory != null)
            return _modelFactory.CreateTrainer(_modelArgument.Value, corpus, maxSize);

        return ToolHelpers.CreateTranslationModelTrainer(
            _modelTypeOption.Value(),
            _modelConfigFileName,
            corpus,
            maxSize
        );
    }
}
