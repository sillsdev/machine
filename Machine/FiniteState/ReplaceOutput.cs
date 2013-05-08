using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class ReplaceOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<ReplaceOutput<TData, TOffset>> where TData : IData<TOffset>
	{
		internal ReplaceOutput(FeatureStruct fs)
			: base(fs)
		{
		}

		public override Annotation<TOffset> UpdateOutput(TData data, Annotation<TOffset> ann, IFstOperations<TData, TOffset> operations)
		{
			ann.FeatureStruct.Clear();
			ann.FeatureStruct.PriorityUnion(FeatureStruct);
			operations.Replace(data, ann);
			return null;
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as ReplaceOutput<TData, TOffset>);
		}

		public bool Equals(ReplaceOutput<TData, TOffset> other)
		{
			return other != null && FeatureStruct.ValueEquals(other.FeatureStruct);
		}

		public override int GetHashCode()
		{
			return FeatureStruct.GetFrozenHashCode();
		}

		public override string ToString()
		{
			return string.Format("({0},↔)", FeatureStruct);
		}
	}
}
