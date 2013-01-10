using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public abstract class Output<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly FeatureStruct _fs;

		protected Output(FeatureStruct fs)
		{
			_fs = fs;
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public abstract void UpdateOutput(TData data, Annotation<TOffset> ann);
	}
}
