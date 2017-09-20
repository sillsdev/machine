using System;
using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
	public class RemoveOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<RemoveOutput<TData, TOffset>> where TData : IAnnotatedData<TOffset>
	{
		internal RemoveOutput()
			: base(null)
		{
		}

		public override Annotation<TOffset> UpdateOutput(TData data, Annotation<TOffset> ann, IFstOperations<TData, TOffset> operations)
		{
			ann.Remove(false);
			operations.Remove(data, ann.Range);
			return null;
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as RemoveOutput<TData, TOffset>);
		}

		public bool Equals(RemoveOutput<TData, TOffset> other)
		{
			return other != null;
		}

		public override int GetHashCode()
		{
			return 1;
		}

		public override string ToString()
		{
			return "ε";
		}
	}
}
