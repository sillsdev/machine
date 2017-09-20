using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;

namespace SIL.Machine.Matching
{
	public class GroupCaptureCollection<TOffset> : IReadOnlyCollection<GroupCapture<TOffset>>
	{
		private readonly Dictionary<string, GroupCapture<TOffset>> _groupCaptures;

		internal GroupCaptureCollection(IEnumerable<GroupCapture<TOffset>> groupCaptures)
		{
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
				return new GroupCapture<TOffset>(groupName, Span<TOffset>.Null);
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
