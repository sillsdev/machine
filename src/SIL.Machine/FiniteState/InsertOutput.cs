using System;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class InsertOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<InsertOutput<TData, TOffset>> where TData : IAnnotatedData<TOffset>
	{
		internal InsertOutput(FeatureStruct fs)
			: base(fs)
		{
		}

		public override Annotation<TOffset> UpdateOutput(TData data, Annotation<TOffset> ann,
			IFstOperations<TData, TOffset> operations)
		{
			Range<TOffset> range = operations.Insert(data, ann, FeatureStruct);
			return data.Annotations.Add(range, FeatureStruct);
		}

		public override bool UsePrevNewAnnotation
		{
			get { return true; }
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
			return string.Format("({0},+)", FeatureStruct);
		}
	}
}
