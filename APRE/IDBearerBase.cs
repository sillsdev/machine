using System;

namespace SIL.APRE
{
	public abstract class IDBearerBase : IIDBearer
	{
		private readonly string _id;

		protected IDBearerBase(string id)
        {
            _id = id;
			Description = id;
        }

		public string ID
		{
			get { return _id;  }
		}

		public string Description { get; set; }

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
