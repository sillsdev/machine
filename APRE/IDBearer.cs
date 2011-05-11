using System;

namespace SIL.APRE
{
	public abstract class IDBearer : IIDBearer
	{
		public static bool operator ==(IDBearer o1, IDBearer o2)
		{
			if (ReferenceEquals(o1, o2))
				return true;
			if ((object)o1 == null || (object)o2 == null)
				return false;
			return o1.Equals(o2);
		}

		public static bool operator !=(IDBearer o1, IDBearer o2)
		{
			return !(o1 == o2);
		}

		private readonly string _id;
		private readonly string _description;

		protected IDBearer(string id, string description)
        {
            _id = id;
            _description = description;
        }

		public string ID
		{
			get { return _id;  }
		}

		public string Description
		{
			get { return _description; }
		}

		public int CompareTo(IIDBearer other)
		{
			if (other == null)
				return 1;
			return _id.CompareTo(other.ID);
		}

		public int CompareTo(object other)
		{
			if (!(other is IIDBearer))
				throw new ArgumentException();
			return CompareTo(other as IIDBearer);
		}

		public override int GetHashCode()
		{
			return _id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as IIDBearer);
		}

		public bool Equals(IIDBearer other)
		{
			if (other == null)
				return false;
			return ID == other.ID;
		}

		public override string ToString()
		{
			return Description;
		}
	}
}
