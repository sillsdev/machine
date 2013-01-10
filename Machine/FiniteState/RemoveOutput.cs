using System;

namespace SIL.Machine.FiniteState
{
	public class RemoveOutput<TData, TOffset> : Output<TData, TOffset>, IEquatable<RemoveOutput<TData, TOffset>> where TData : IData<TOffset>
	{
		private readonly Action<TData, Span<TOffset>> _updateData;

		internal RemoveOutput(Action<TData, Span<TOffset>> updateData)
			: base(null)
		{
			_updateData = updateData;
		}

		public override void UpdateOutput(TData data, Annotation<TOffset> ann)
		{
			ann.Remove(false);
			_updateData(data, ann.Span);
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
