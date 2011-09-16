using System;

namespace SIL.APRE.Fsa
{
	internal class TagMapCommand : IComparable<TagMapCommand>
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

		public override string ToString()
		{
			return string.Format("{0} <- {1}", Dest, Src == CurrentPosition ? "p" : Src.ToString());
		}
	}
}
