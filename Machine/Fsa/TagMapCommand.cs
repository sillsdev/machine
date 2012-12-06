using System;
using System.Globalization;

namespace SIL.Machine.Fsa
{
	internal class TagMapCommand : IComparable<TagMapCommand>, IEquatable<TagMapCommand>
	{
		public const int CurrentPosition = -1;

		public TagMapCommand(int dest, int src)
		{
			Dest = dest;
			Src = src;
		}

		public int Dest { get; set; }

		public int Src { get; set; }

		public int CompareTo(TagMapCommand other)
		{
			if (Src == CurrentPosition && other.Src != CurrentPosition)
				return 1;
			if (Src != CurrentPosition && other.Src == CurrentPosition)
				return -1;
			if (Dest < other.Dest)
				return -1;
			if (Dest > other.Dest)
				return 1;
			return 0;
		}

		public bool Equals(TagMapCommand other)
		{
			return other != null && Dest == other.Dest && Src == other.Src;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as TagMapCommand);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + Dest;
			code = code * 31 + Src;
			return code;
		}

		public override string ToString()
		{
			return string.Format("{0} <- {1}", Dest, Src == CurrentPosition ? "p" : Src.ToString(CultureInfo.InvariantCulture));
		}
	}
}
