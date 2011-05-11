using System;

namespace SIL.APRE.Fsa
{
	internal struct TagMapCommand : IComparable<TagMapCommand>
	{
		public const int CurrentPosition = -1;

		private int _dest;
		private int _src;

		public TagMapCommand(int dest, int src)
		{
			_dest = dest;
			_src = src;
		}

		public int Dest
		{
			get
			{
				return _dest;
			}

			set
			{
				_dest = value;
			}
		}

		public int Src
		{
			get
			{
				return _src;
			}

			set
			{
				_src = value;
			}
		}

		public int CompareTo(TagMapCommand other)
		{
			if (_dest < other._dest)
				return -1;
			if (_dest > other._dest)
				return 1;
			return 0;
		}

		public override string ToString()
		{
			return string.Format("{0} <- {1}", _dest, _src == CurrentPosition ? "p" : _src.ToString());
		}
	}
}
