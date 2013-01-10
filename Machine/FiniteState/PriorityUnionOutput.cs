using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class PriorityUnionOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<PriorityUnionOutput<TData, TOffset>> where TData : IData<TOffset>
	{
		private readonly Action<TData, Annotation<TOffset>> _updateData; 

		internal PriorityUnionOutput(FeatureStruct fs)
			: this(fs, (data, annotation) => { })
		{
		}

		internal PriorityUnionOutput(FeatureStruct fs, Action<TData, Annotation<TOffset>> updateData)
			: base(fs)
		{
			_updateData = updateData;
		}

		public override void UpdateOutput(TData data, Annotation<TOffset> ann)
		{
			ann.FeatureStruct.PriorityUnion(FeatureStruct);
			_updateData(data, ann);
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
			return FeatureStruct.GetFrozenHashCode();
		}

		public override string ToString()
		{
			return FeatureStruct.ToString();
		}
	}
}
