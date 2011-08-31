using System;

namespace SIL.APRE.Fsa
{
	public class AcceptInfo<TOffset>
	{
		private readonly string _id;
		private readonly Func<IBidirList<Annotation<TOffset>>, bool> _accept;

		internal AcceptInfo(string id, Func<IBidirList<Annotation<TOffset>>, bool> accept)
		{
			_id = id;
			_accept = accept;
		}

		public string ID
		{
			get { return _id; }
		}

		public Func<IBidirList<Annotation<TOffset>>, bool> Accept
		{
			get { return _accept; }
		}
	}
}
