using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.FiniteState
{
	public struct Register<TOffset>
	{
		private TOffset _offset;
		private bool _hasOffset;
		private bool _start;

		public Register(TOffset offset, bool start)
		{
			_offset = offset;
			_start = start;
			_hasOffset = true;
		}

		public bool HasOffset
		{
			get { return _hasOffset; }
			set
			{
				_hasOffset = value;
				if (!_hasOffset)
					_offset = default(TOffset);
			}
		}

		public void SetOffset(TOffset value, bool start)
		{
			_offset = value;
			_start = start;
			_hasOffset = true;
		}

		public TOffset Offset
		{
			get { return _offset; }
		}

		public bool IsStart
		{
			get { return _start; }
		}

		public bool ValueEquals(Register<TOffset> other, IEqualityComparer<TOffset> offsetComparer)
		{
			if (_hasOffset != other._hasOffset)
				return false;

			return !_hasOffset || (offsetComparer.Equals(_offset, other._offset) && _start == other._start);
		}

		public override string ToString()
		{
			if (_hasOffset)
			{
				var sb = new StringBuilder();
				if (!_start)
					sb.Append(":");
				sb.Append(_offset);
				if (_start)
					sb.Append(":");
				return sb.ToString();
			}
			return "null";
		}
	}
}
