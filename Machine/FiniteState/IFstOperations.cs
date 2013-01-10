using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public interface IFstOperations<in TData, TOffset>
	{
		void Replace(TData data, Annotation<TOffset> ann);

		Span<TOffset> Insert(TData data, Annotation<TOffset> ann, FeatureStruct fs);

		void Remove(TData data, Span<TOffset> span);
	}
}
