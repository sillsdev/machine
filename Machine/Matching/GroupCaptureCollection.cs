using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Matching
{
	public class GroupCaptureCollection<TOffset> : IReadOnlyCollection<GroupCapture<TOffset>>
	{
		private readonly SpanFactory<TOffset> _spanFactory; 
		private readonly Dictionary<string, GroupCapture<TOffset>> _groupCaptures;

		internal GroupCaptureCollection(SpanFactory<TOffset> spanFactory, IEnumerable<GroupCapture<TOffset>> groupCaptures)
		{
			_spanFactory = spanFactory;
			_groupCaptures = groupCaptures.ToDictionary(capture => capture.Name);
		}

		public IEnumerator<GroupCapture<TOffset>> GetEnumerator()
		{
			return _groupCaptures.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count
		{
			get { return _groupCaptures.Count; }
		}

		public GroupCapture<TOffset> this[string groupName]
		{
			get
			{
				GroupCapture<TOffset> capture;
				if (_groupCaptures.TryGetValue(groupName, out capture))
					return capture;
				return new GroupCapture<TOffset>(groupName, _spanFactory.Empty);
			}
		}

		public bool Captured(string groupName)
		{
			GroupCapture<TOffset> capture;
			if (_groupCaptures.TryGetValue(groupName, out capture))
				return capture.Success;
			return false;
		}
	}
}
