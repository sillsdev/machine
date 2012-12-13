using System;

namespace SIL.Machine.Fsa
{
	public class AcceptInfo<TData, TOffset, TResult> : IEquatable<AcceptInfo<TData, TOffset, TResult>> where TData : IData<TOffset>
	{
		private readonly string _id;
		private readonly Func<TData, TResult, bool> _acceptable;
		private readonly int _priority;

		public AcceptInfo(string id, Func<TData, TResult, bool> acceptable, int priority)
		{
			_id = id;
			_acceptable = acceptable;
			_priority = priority;
		}

		public string ID
		{
			get { return _id; }
		}

		public Func<TData, TResult, bool> Acceptable
		{
			get { return _acceptable; }
		}

		public int Priority
		{
			get { return _priority; }
		}

		public bool Equals(AcceptInfo<TData, TOffset, TResult> other)
		{
			return other != null && _id == other._id && _acceptable == other._acceptable;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as AcceptInfo<TData, TOffset, TResult>);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + (_id == null ? 0 : _id.GetHashCode());
			code = code * 31 + _acceptable.GetHashCode();
			return code;
		}
	}
}
