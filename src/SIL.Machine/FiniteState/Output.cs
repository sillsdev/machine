using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public abstract class Output<TData, TOffset> where TData : IAnnotatedData<TOffset>
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

		public abstract Annotation<TOffset> UpdateOutput(TData data, Annotation<TOffset> ann, IFstOperations<TData, TOffset> operations);

		public virtual bool UsePrevNewAnnotation
		{
			get { return false; }
		}
	}
}
