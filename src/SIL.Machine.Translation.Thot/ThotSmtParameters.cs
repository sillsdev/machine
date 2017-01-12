using System.Collections.Generic;
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

		public ThotSmtParameters()
		{
		}

		private ThotSmtParameters(ThotSmtParameters other)
		{
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

		public uint ModelNonMonotonicity
		{
			get { return _modelNonMonotonicity; }
			set
			{
				CheckFrozen();
				_modelNonMonotonicity = value;
			}
		}

		public float ModelW
		{
			get { return _modelW; }
			set
			{
				CheckFrozen();
				_modelW = value;
			}
		}

		public uint ModelA
		{
			get { return _modelA; }
			set
			{
				CheckFrozen();
				_modelA = value;
			}
		}

		public uint ModelE
		{
			get { return _modelE; }
			set
			{
				CheckFrozen();
				_modelE = value;
			}
		}

		public ModelHeuristic ModelHeuristic
		{
			get { return _modelHeuristic; }
			set
			{
				CheckFrozen();
				_modelHeuristic = value;
			}
		}

		public IReadOnlyList<float> ModelWeights
		{
			get { return _modelWeights; }
			set
			{
				CheckFrozen();
				_modelWeights = value;
			}
		}

		public LearningAlgorithm LearningAlgorithm
		{
			get { return _learningAlgorithm; }
			set
			{
				CheckFrozen();
				_learningAlgorithm = value;
			}
		}

		public LearningRatePolicy LearningRatePolicy
		{
			get { return _learningRatePolicy; }
			set
			{
				CheckFrozen();
				_learningRatePolicy = value;
			}
		}

		public float LearningStepSize
		{
			get { return _learningStepSize; }
			set
			{
				CheckFrozen();
				_learningStepSize = value;
			}
		}

		public uint LearningEMIters
		{
			get { return _learningEMIters; }
			set
			{
				CheckFrozen();
				_learningEMIters = value;
			}
		}

		public uint LearningE
		{
			get { return _learningE; }
			set
			{
				CheckFrozen();
				_learningE = value;
			}
		}

		public uint LearningR
		{
			get { return _learningR; }
			set
			{
				CheckFrozen();
				_learningR = value;
			}
		}

		public uint DecoderS
		{
			get { return _decoderS; }
			set
			{
				CheckFrozen();
				_decoderS = value;
			}
		}

		public bool DecoderBreadthFirst
		{
			get { return _decoderBreadthFirst; }
			set
			{
				CheckFrozen();
				_decoderBreadthFirst = value;
			}
		}

		public uint DecoderG
		{
			get { return _decoderG; }
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

			return _modelNonMonotonicity == other._modelNonMonotonicity && _modelW == other._modelW && _modelA == other._modelA
				&& _modelE == other._modelE && _modelHeuristic == other._modelHeuristic
				&& _learningAlgorithm == other._learningAlgorithm && _learningRatePolicy == other._learningRatePolicy
				&& _learningStepSize == other._learningStepSize && _learningEMIters == other._learningEMIters && _learningE == other._learningE
				&& _learningR == other._learningR
				&& _decoderS == other._decoderS && _decoderBreadthFirst == other._decoderBreadthFirst && _decoderG == other._decoderG;
		}

		protected override int FreezeImpl()
		{
			int code = 23;
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
