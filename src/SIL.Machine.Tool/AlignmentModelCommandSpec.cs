using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Plugin;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using YamlDotNet.RepresentationModel;

namespace SIL.Machine
{
    public class AlignmentModelCommandSpec : ICommandSpec
    {
        private CommandArgument _modelArgument;
        private CommandOption _modelTypeOption;
        private CommandOption _pluginOption;

        private IWordAlignmentModelFactory _modelFactory;

        public bool IsSymmetric
        {
            get
            {
                if (_modelFactory != null)
                    return _modelFactory.IsSymmetric;

                return false;
            }
        }

        public void AddParameters(CommandBase command)
        {
            _modelArgument = command.Argument("MODEL_PATH", "The word alignment model.").IsRequired();
            _modelTypeOption = command.Option(
                "-mt|--model-type <MODEL_TYPE>",
                $"The word alignment model type.\nTypes: \"{ToolHelpers.Hmm}\" (default), \"{ToolHelpers.Ibm1}\", \"{ToolHelpers.Ibm2}\", \"{ToolHelpers.Ibm3}\", \"{ToolHelpers.Ibm4}\", \"{ToolHelpers.FastAlign}\".",
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
            var factories = pluginLoader.Create<IWordAlignmentModelFactory>().ToDictionary(f => f.ModelType);

            if (!ValidateAlignmentModelTypeOption(_modelTypeOption.Value(), factories.Keys))
            {
                outWriter.WriteLine("The specified word alignment model type is invalid.");
                return false;
            }

            if (
                _modelTypeOption.HasValue()
                && factories.TryGetValue(_modelTypeOption.Value(), out IWordAlignmentModelFactory factory)
            )
            {
                _modelFactory = factory;
            }

            return true;
        }

        public IWordAlignmentModel CreateAlignmentModel(
            WordAlignmentDirection direction = WordAlignmentDirection.Symmetric,
            SymmetrizationHeuristic symHeuristic = SymmetrizationHeuristic.Och
        )
        {
            if (_modelFactory != null)
                return _modelFactory.CreateModel(_modelArgument.Value, direction, symHeuristic);

            string modelPath = _modelArgument.Value;
            if (ToolHelpers.IsDirectoryPath(modelPath))
                modelPath = Path.Combine(modelPath, "src_trg");

            ThotWordAlignmentModelType modelType = ThotWordAlignmentModelType.Hmm;
            if (_modelTypeOption.HasValue())
            {
                modelType = ToolHelpers.GetThotWordAlignmentModelType(_modelTypeOption.Value());
            }
            else
            {
                string configPath = modelPath + "_invswm.yml";
                if (File.Exists(configPath))
                {
                    using (var reader = new StreamReader(configPath))
                    {
                        var yaml = new YamlStream();
                        yaml.Load(reader);
                        var root = (YamlMappingNode)yaml.Documents.First().RootNode;
                        var modelTypeStr = (string)root[new YamlScalarNode("model")];
                        modelType = ToolHelpers.GetThotWordAlignmentModelType(modelTypeStr);
                    }
                }
            }

            if (direction == WordAlignmentDirection.Direct)
            {
                var directModel = ThotWordAlignmentModel.Create(modelType);
                directModel.Load(modelPath + "_invswm");
                return directModel;
            }
            else if (direction == WordAlignmentDirection.Inverse)
            {
                var inverseModel = ThotWordAlignmentModel.Create(modelType);
                inverseModel.Load(modelPath + "_swm");
                return inverseModel;
            }
            else
            {
                var directModel = ThotWordAlignmentModel.Create(modelType);
                directModel.Load(modelPath + "_invswm");

                var inverseModel = ThotWordAlignmentModel.Create(modelType);
                inverseModel.Load(modelPath + "_swm");

                return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = symHeuristic };
            }
        }

        public ITrainer CreateAlignmentModelTrainer(
            IParallelTextCorpus corpus,
            int maxSize,
            Dictionary<string, string> parameters,
            bool direct = true
        )
        {
            if (_modelFactory != null)
            {
                return _modelFactory.CreateTrainer(_modelArgument.Value, corpus, maxSize, parameters, direct);
            }

            ThotWordAlignmentModelType modelType = ToolHelpers.GetThotWordAlignmentModelType(_modelTypeOption.Value());

            string modelPath = _modelArgument.Value;
            if (ToolHelpers.IsDirectoryPath(modelPath))
                modelPath = Path.Combine(modelPath, "src_trg");
            string modelDir = Path.GetDirectoryName(modelPath);
            if (!Directory.Exists(modelDir))
                Directory.CreateDirectory(modelDir);

            string modelStr;
            IParallelTextCorpus trainCorpus;
            if (direct)
            {
                modelStr = "invswm";
                trainCorpus = corpus;
            }
            else
            {
                modelStr = "swm";
                trainCorpus = corpus.Invert();
            }

            var thotParameters = new ThotWordAlignmentParameters();
            SetThotParameter(parameters, thotParameters, "fa-iters", p => p.FastAlignIterationCount);
            SetThotParameter(parameters, thotParameters, "ibm1-iters", p => p.Ibm1IterationCount);
            SetThotParameter(parameters, thotParameters, "ibm2-iters", p => p.Ibm2IterationCount);
            SetThotParameter(parameters, thotParameters, "hmm-iters", p => p.HmmIterationCount);
            SetThotParameter(parameters, thotParameters, "ibm3-iters", p => p.Ibm3IterationCount);
            SetThotParameter(parameters, thotParameters, "ibm4-iters", p => p.Ibm4IterationCount);
            SetThotParameter(parameters, thotParameters, "var-bayes", p => p.VariationalBayes);
            SetThotParameter(parameters, thotParameters, "fa-p0", p => p.FastAlignP0);
            SetThotParameter(parameters, thotParameters, "hmm-p0", p => p.HmmP0);
            SetThotParameter(parameters, thotParameters, "hmm-lex-smooth", p => p.HmmLexicalSmoothingFactor);
            SetThotParameter(parameters, thotParameters, "hmm-align-smooth", p => p.HmmAlignmentSmoothingFactor);
            SetThotParameter(parameters, thotParameters, "ibm3-fert-smooth", p => p.Ibm3FertilitySmoothingFactor);
            SetThotParameter(parameters, thotParameters, "ibm3-count-threshold", p => p.Ibm3CountThreshold);
            SetThotParameter(parameters, thotParameters, "ibm4-dist-smooth", p => p.Ibm4DistortionSmoothingFactor);

            if (parameters.TryGetValue("src-classes", out string srcClassesFileName))
            {
                IReadOnlyDictionary<string, string> wordClasses = LoadWordClasses(srcClassesFileName);
                if (direct)
                    thotParameters.SourceWordClasses = wordClasses;
                else
                    thotParameters.TargetWordClasses = wordClasses;
            }
            if (parameters.TryGetValue("trg-classes", out string trgClassesFileName))
            {
                IReadOnlyDictionary<string, string> wordClasses = LoadWordClasses(trgClassesFileName);
                if (direct)
                    thotParameters.TargetWordClasses = wordClasses;
                else
                    thotParameters.SourceWordClasses = wordClasses;
            }

            return new ThotWordAlignmentModelTrainer(modelType, trainCorpus, $"{modelPath}_{modelStr}", thotParameters)
            {
                MaxCorpusCount = maxSize
            };
        }

        private static void SetThotParameter<T>(
            Dictionary<string, string> input,
            ThotWordAlignmentParameters parameters,
            string name,
            Expression<Func<ThotWordAlignmentParameters, T>> propExpr
        )
        {
            if (input.TryGetValue(name, out string valueStr))
            {
                var expr = (MemberExpression)propExpr.Body;
                var prop = (PropertyInfo)expr.Member;
                Type type = typeof(T);
                if (Nullable.GetUnderlyingType(type) != null)
                    type = Nullable.GetUnderlyingType(type);
                prop.SetValue(parameters, (T)Convert.ChangeType(valueStr, type));
            }
        }

        private static bool ValidateAlignmentModelTypeOption(string value, IEnumerable<string> pluginTypes)
        {
            var validTypes = new HashSet<string>
            {
                ToolHelpers.Hmm,
                ToolHelpers.Ibm1,
                ToolHelpers.Ibm2,
                ToolHelpers.FastAlign,
                ToolHelpers.Ibm3,
                ToolHelpers.Ibm4
            };
            validTypes.UnionWith(pluginTypes);
            return string.IsNullOrEmpty(value) || validTypes.Contains(value);
        }

        private static IReadOnlyDictionary<string, string> LoadWordClasses(string fileName)
        {
            var wordClasses = new Dictionary<string, string>();
            using var reader = new StreamReader(fileName);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                string[] parts = line.Split("\t", 2);
                wordClasses[parts[0]] = parts[1];
            }
            return wordClasses;
        }
    }
}
