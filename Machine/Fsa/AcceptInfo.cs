using System;

namespace SIL.Machine.Fsa
{
	public class AcceptInfo<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly string _id;
		private readonly Func<TData, FsaMatch<TOffset>, bool> _acceptable;
		private readonly int _priority;

		public AcceptInfo(string id, Func<TData, FsaMatch<TOffset>, bool> acceptable, int priority)
		{
			_id = id;
			_acceptable = acceptable;
			_priority = priority;
		}

		public string ID
		{
			get { return _id; }
		}

		public Func<TData, FsaMatch<TOffset>, bool> Acceptable
		{
			get { return _acceptable; }
		}

		public int Priority
		{
			get { return _priority; }
		}
	}
}
