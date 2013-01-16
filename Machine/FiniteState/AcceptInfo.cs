using System;

namespace SIL.Machine.FiniteState
{
	public class AcceptInfo<TData, TOffset> : IEquatable<AcceptInfo<TData, TOffset>> where TData : IData<TOffset>
	{
		private readonly string _id;
		private readonly Func<TData, FstResult<TData, TOffset>, bool> _acceptable;
		private readonly int _priority;

		public AcceptInfo(string id, Func<TData, FstResult<TData, TOffset>, bool> acceptable, int priority)
		{
			_id = id;
			_acceptable = acceptable;
			_priority = priority;
		}

		public string ID
		{
			get { return _id; }
		}

		public Func<TData, FstResult<TData, TOffset>, bool> Acceptable
		{
			get { return _acceptable; }
		}

		public int Priority
		{
			get { return _priority; }
		}

		public bool Equals(AcceptInfo<TData, TOffset> other)
		{
			return other != null && _id == other._id && _acceptable == other._acceptable;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as AcceptInfo<TData, TOffset>);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + (_id == null ? 0 : _id.GetHashCode());
			if (_acceptable != null)
				code = code * 31 + _acceptable.GetHashCode();
			return code;
		}
	}
}
