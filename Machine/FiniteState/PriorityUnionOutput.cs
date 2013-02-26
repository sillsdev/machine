using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class PriorityUnionOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<PriorityUnionOutput<TData, TOffset>> where TData : IData<TOffset>
	{
		internal PriorityUnionOutput(FeatureStruct fs)
			: base(fs)
		{
		}

		public override Annotation<TOffset> UpdateOutput(TData data, Annotation<TOffset> ann, IFstOperations<TData, TOffset> operations)
		{
			ann.FeatureStruct.PriorityUnion(FeatureStruct);
			if (!FeatureStruct.IsEmpty)
				operations.Replace(data, ann);
			return null;
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as PriorityUnionOutput<TData, TOffset>);
		}

		public bool Equals(PriorityUnionOutput<TData, TOffset> other)
		{
			return other != null && FeatureStruct.ValueEquals(other.FeatureStruct);
		}

		public override int GetHashCode()
		{
			return FeatureStruct.GetValueHashCode();
		}

		public override string ToString()
		{
			return string.Format("({0},∪)", FeatureStruct);
		}
	}
}
