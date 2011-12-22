namespace SIL.Machine
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

		public override string ToString()
		{
			return Description;
		}
	}
}
