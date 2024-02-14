using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
    public enum ModelHeuristic : uint
    {
        NoHeuristic = 0,
        LocalT = 4,
        LocalTD = 6
    }

    public enum LearningAlgorithm : uint
    {
        BasicIncrementalTraining = 0,
        MinibatchTraining = 1,
        BatchRetraining = 2
    }

    public enum LearningRatePolicy : uint
    {
        Fixed = 0,
        Liang = 1,
        Own = 2,
        WerBased = 3
    }

    public class ThotSmtParameters : Freezable<ThotSmtParameters>, ICloneable<ThotSmtParameters>
    {
        public static ThotSmtParameters Load(string cfgFileName)
        {
            var parameters = new ThotSmtParameters();
            string cfgDirPath = Path.GetDirectoryName(cfgFileName);
            foreach (string line in File.ReadAllLines(cfgFileName))
            {
                string name;
                string value;
                if (!GetConfigParameter(line, out name, out value))
                    continue;

                switch (name)
                {
                    case "tm":
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new ArgumentException(
                                "The -tm parameter does not have a value.",
                                nameof(cfgFileName)
                            );
                        }

                        parameters.TranslationModelFileNamePrefix = value;
                        if (
                            !Path.IsPathRooted(parameters.TranslationModelFileNamePrefix)
                            && !string.IsNullOrEmpty(cfgDirPath)
                        )
                        {
                            parameters.TranslationModelFileNamePrefix = Path.Combine(
                                cfgDirPath,
                                parameters.TranslationModelFileNamePrefix
                            );
                        }

                        break;
                    case "lm":
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new ArgumentException(
                                "The -lm parameter does not have a value.",
                                nameof(cfgFileName)
                            );
                        }

                        parameters.LanguageModelFileNamePrefix = value;
                        if (
                            !Path.IsPathRooted(parameters.LanguageModelFileNamePrefix)
                            && !string.IsNullOrEmpty(cfgDirPath)
                        )
                        {
                            parameters.LanguageModelFileNamePrefix = Path.Combine(
                                cfgDirPath,
                                parameters.LanguageModelFileNamePrefix
                            );
                        }

                        break;
                    case "W":
                        if (string.IsNullOrEmpty(value))
                            throw new ArgumentException("The -W parameter does not have a value.", nameof(cfgFileName));
                        parameters.ModelW = float.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "S":
                        if (string.IsNullOrEmpty(value))
                            throw new ArgumentException("The -S parameter does not have a value.", nameof(cfgFileName));
                        parameters.DecoderS = uint.Parse(value);
                        break;
                    case "A":
                        if (string.IsNullOrEmpty(value))
                            throw new ArgumentException("The -A parameter does not have a value.", nameof(cfgFileName));
                        parameters.ModelA = uint.Parse(value);
                        break;
                    case "E":
                        if (string.IsNullOrEmpty(value))
                            throw new ArgumentException("The -E parameter does not have a value.", nameof(cfgFileName));
                        parameters.ModelE = uint.Parse(value);
                        break;
                    case "nomon":
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new ArgumentException(
                                "The -nomon parameter does not have a value.",
                                nameof(cfgFileName)
                            );
                        }

                        parameters.ModelNonMonotonicity = uint.Parse(value);
                        break;
                    case "be":
                        parameters.DecoderBreadthFirst = false;
                        break;
                    case "G":
                        if (string.IsNullOrEmpty(value))
                            throw new ArgumentException("The -G parameter does not have a value.", nameof(cfgFileName));
                        parameters.DecoderG = uint.Parse(value);
                        break;
                    case "h":
                        if (string.IsNullOrEmpty(value))
                            throw new ArgumentException("The -h parameter does not have a value.", nameof(cfgFileName));
                        parameters.ModelHeuristic = (ModelHeuristic)uint.Parse(value);
                        break;
                    case "olp":
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new ArgumentException(
                                "The -olp parameter does not have a value.",
                                nameof(cfgFileName)
                            );
                        }

                        string[] tokens = value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length >= 1)
                            parameters.LearningAlgorithm = (LearningAlgorithm)uint.Parse(tokens[0]);
                        if (tokens.Length >= 2)
                            parameters.LearningRatePolicy = (LearningRatePolicy)uint.Parse(tokens[1]);
                        if (tokens.Length >= 3)
                            parameters.LearningStepSize = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                        if (tokens.Length >= 4)
                            parameters.LearningEMIters = uint.Parse(tokens[3]);
                        if (tokens.Length >= 5)
                            parameters.LearningE = uint.Parse(tokens[4]);
                        if (tokens.Length >= 6)
                            parameters.LearningR = uint.Parse(tokens[5]);
                        break;
                    case "tmw":
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new ArgumentException(
                                "The -tmw parameter does not have a value.",
                                nameof(cfgFileName)
                            );
                        }

                        parameters.ModelWeights = value
                            .Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => float.Parse(t, CultureInfo.InvariantCulture))
                            .ToArray();
                        break;
                }
            }

            return parameters;
        }

        internal static bool GetConfigParameter(string line, out string name, out string value)
        {
            name = null;
            value = null;
            string l = line.Trim();
            if (l.StartsWith("#"))
                return false;

            int index = l.IndexOf(' ');
            if (index == -1)
            {
                name = l;
            }
            else
            {
                name = l.Substring(0, index);
                value = l.Substring(index + 1).Trim();
            }

            if (name.StartsWith("-"))
                name = name.Substring(1);
            return true;
        }

        private string _tmFileNamePrefix;
        private string _lmFileNamePrefix;
        private uint _modelNonMonotonicity;
        private float _modelW = 0.4f;
        private uint _modelA = 10;
        private uint _modelE = 2;
        private ModelHeuristic _modelHeuristic = ModelHeuristic.LocalTD;
        private IReadOnlyList<float> _modelWeights;
        private LearningAlgorithm _learningAlgorithm;
        private LearningRatePolicy _learningRatePolicy;
        private float _learningStepSize = 1;
        private uint _learningEMIters = 5;
        private uint _learningE = 1;
        private uint _learningR;
        private uint _decoderS = 10;
        private bool _decoderBreadthFirst = true;
        private uint _decoderG;

        public ThotSmtParameters() { }

        private ThotSmtParameters(ThotSmtParameters other)
        {
            _tmFileNamePrefix = other._tmFileNamePrefix;
            _lmFileNamePrefix = other._lmFileNamePrefix;
            _modelNonMonotonicity = other._modelNonMonotonicity;
            _modelW = other._modelW;
            _modelA = other._modelA;
            _modelE = other._modelE;
            _modelHeuristic = other._modelHeuristic;
            _modelWeights = other._modelWeights;
            _learningAlgorithm = other._learningAlgorithm;
            _learningRatePolicy = other._learningRatePolicy;
            _learningStepSize = other._learningStepSize;
            _learningEMIters = other._learningEMIters;
            _learningE = other._learningE;
            _learningR = other._learningR;
            _decoderS = other._decoderS;
            _decoderBreadthFirst = other._decoderBreadthFirst;
            _decoderG = other._decoderG;
        }

        public string TranslationModelFileNamePrefix
        {
            get => _tmFileNamePrefix;
            set
            {
                CheckFrozen();
                _tmFileNamePrefix = value;
            }
        }

        public string LanguageModelFileNamePrefix
        {
            get => _lmFileNamePrefix;
            set
            {
                CheckFrozen();
                _lmFileNamePrefix = value;
            }
        }

        public uint ModelNonMonotonicity
        {
            get => _modelNonMonotonicity;
            set
            {
                CheckFrozen();
                _modelNonMonotonicity = value;
            }
        }

        public float ModelW
        {
            get => _modelW;
            set
            {
                CheckFrozen();
                _modelW = value;
            }
        }

        public uint ModelA
        {
            get => _modelA;
            set
            {
                CheckFrozen();
                _modelA = value;
            }
        }

        public uint ModelE
        {
            get => _modelE;
            set
            {
                CheckFrozen();
                _modelE = value;
            }
        }

        public ModelHeuristic ModelHeuristic
        {
            get => _modelHeuristic;
            set
            {
                CheckFrozen();
                _modelHeuristic = value;
            }
        }

        public IReadOnlyList<float> ModelWeights
        {
            get => _modelWeights;
            set
            {
                CheckFrozen();
                _modelWeights = value;
            }
        }

        public LearningAlgorithm LearningAlgorithm
        {
            get => _learningAlgorithm;
            set
            {
                CheckFrozen();
                _learningAlgorithm = value;
            }
        }

        public LearningRatePolicy LearningRatePolicy
        {
            get => _learningRatePolicy;
            set
            {
                CheckFrozen();
                _learningRatePolicy = value;
            }
        }

        public float LearningStepSize
        {
            get => _learningStepSize;
            set
            {
                CheckFrozen();
                _learningStepSize = value;
            }
        }

        public uint LearningEMIters
        {
            get => _learningEMIters;
            set
            {
                CheckFrozen();
                _learningEMIters = value;
            }
        }

        public uint LearningE
        {
            get => _learningE;
            set
            {
                CheckFrozen();
                _learningE = value;
            }
        }

        public uint LearningR
        {
            get => _learningR;
            set
            {
                CheckFrozen();
                _learningR = value;
            }
        }

        public uint DecoderS
        {
            get => _decoderS;
            set
            {
                CheckFrozen();
                _decoderS = value;
            }
        }

        public bool DecoderBreadthFirst
        {
            get => _decoderBreadthFirst;
            set
            {
                CheckFrozen();
                _decoderBreadthFirst = value;
            }
        }

        public uint DecoderG
        {
            get => _decoderG;
            set
            {
                CheckFrozen();
                _decoderG = value;
            }
        }

        public override bool ValueEquals(ThotSmtParameters other)
        {
            if (other == null)
                return false;

            if (_modelWeights == null || other._modelWeights == null)
            {
                if (_modelWeights != other._modelWeights)
                    return false;
            }
            else if (!_modelWeights.SequenceEqual(other._modelWeights))
            {
                return false;
            }

            return _tmFileNamePrefix == other._tmFileNamePrefix
                && _lmFileNamePrefix == other._lmFileNamePrefix
                && _modelNonMonotonicity == other._modelNonMonotonicity
                && _modelW == other._modelW
                && _modelA == other._modelA
                && _modelE == other._modelE
                && _modelHeuristic == other._modelHeuristic
                && _learningAlgorithm == other._learningAlgorithm
                && _learningRatePolicy == other._learningRatePolicy
                && _learningStepSize == other._learningStepSize
                && _learningEMIters == other._learningEMIters
                && _learningE == other._learningE
                && _learningR == other._learningR
                && _decoderS == other._decoderS
                && _decoderBreadthFirst == other._decoderBreadthFirst
                && _decoderG == other._decoderG;
        }

        protected override int FreezeImpl()
        {
            int code = 23;
            code = code * 31 + _tmFileNamePrefix.GetHashCode();
            code = code * 31 + _lmFileNamePrefix.GetHashCode();
            code = code * 31 + _modelNonMonotonicity.GetHashCode();
            code = code * 31 + _modelW.GetHashCode();
            code = code * 31 + _modelA.GetHashCode();
            code = code * 31 + _modelE.GetHashCode();
            code = code * 31 + _modelHeuristic.GetHashCode();
            code = code * 31 + _modelWeights?.GetSequenceHashCode() ?? 0;
            code = code * 31 + _learningAlgorithm.GetHashCode();
            code = code * 31 + _learningRatePolicy.GetHashCode();
            code = code * 31 + _learningStepSize.GetHashCode();
            code = code * 31 + _learningEMIters.GetHashCode();
            code = code * 31 + _learningE.GetHashCode();
            code = code * 31 + _learningR.GetHashCode();
            code = code * 31 + _decoderS.GetHashCode();
            code = code * 31 + _decoderBreadthFirst.GetHashCode();
            code = code * 31 + _decoderG.GetHashCode();
            return code;
        }

        public ThotSmtParameters Clone()
        {
            return new ThotSmtParameters(this);
        }
    }
}
