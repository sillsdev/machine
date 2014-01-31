using SIL.Machine.Annotations;

namespace SIL.Machine.Matching
{
	public class GroupCapture<TOffset>
	{
		private readonly string _name;
		private readonly Span<TOffset> _span;

		internal GroupCapture(string name, Span<TOffset> span)
		{
			_name = name;
			_span = span;
		}

		public string Name
		{
			get { return _name; }
		}

		public Span<TOffset> Span
		{
			get { return _span; }
		}

		public bool Success
		{
			get { return !_span.IsEmpty; }
		}
	}
}
