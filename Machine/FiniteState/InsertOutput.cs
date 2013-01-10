using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class InsertOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<InsertOutput<TData, TOffset>> where TData : IData<TOffset>
	{
		private readonly Func<TData, Annotation<TOffset>, FeatureStruct, Span<TOffset>> _updateData;

		internal InsertOutput(FeatureStruct fs, Func<TData, Annotation<TOffset>, FeatureStruct, Span<TOffset>> updateData)
			: base(fs)
		{
			_updateData = updateData;
		}

		public override void UpdateOutput(TData data, Annotation<TOffset> ann)
		{
			Span<TOffset> span = _updateData(data, ann, FeatureStruct);
			data.Annotations.Add(span, FeatureStruct);
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as InsertOutput<TData, TOffset>);
		}

		public bool Equals(InsertOutput<TData, TOffset> other)
		{
			return other != null && FeatureStruct.ValueEquals(other.FeatureStruct);
		}

		public override int GetHashCode()
		{
			return FeatureStruct.GetFrozenHashCode();
		}

		public override string ToString()
		{
			return string.Format("{0}/I", FeatureStruct);
		}
	}
}
