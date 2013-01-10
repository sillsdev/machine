using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class ReplaceOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<ReplaceOutput<TData, TOffset>> where TData : IData<TOffset>
	{
		private readonly Action<TData, Annotation<TOffset>> _updateData; 

		internal ReplaceOutput(FeatureStruct fs)
			: this(fs, (data, annotation) => { })
		{
		}

		internal ReplaceOutput(FeatureStruct fs, Action<TData, Annotation<TOffset>> updateData)
			: base(fs)
		{
			_updateData = updateData;
		}

		public override void UpdateOutput(TData data, Annotation<TOffset> ann)
		{
			ann.FeatureStruct.Clear();
			ann.FeatureStruct.PriorityUnion(FeatureStruct);
			_updateData(data, ann);
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
			return string.Format("{0}/R", FeatureStruct);
		}
	}
}
