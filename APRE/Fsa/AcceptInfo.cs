using System;

namespace SIL.APRE.Fsa
{
	public class AcceptInfo<TOffset>
	{
		private readonly string _id;
		private readonly Func<IBidirList<Annotation<TOffset>>, FsaMatch<TOffset>, bool> _acceptable;
		private readonly int _priority;

		internal AcceptInfo(string id, Func<IBidirList<Annotation<TOffset>>, FsaMatch<TOffset>, bool> acceptable, int priority)
		{
			_id = id;
			_acceptable = acceptable;
			_priority = priority;
		}

		public string ID
		{
			get { return _id; }
		}

		public Func<IBidirList<Annotation<TOffset>>, FsaMatch<TOffset>, bool> Acceptable
		{
			get { return _acceptable; }
		}

		public int Priority
		{
			get { return _priority; }
		}
	}
}
