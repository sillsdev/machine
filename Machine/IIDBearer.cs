using System;

namespace SIL.Machine
{
	public interface IIDBearer : IComparable<IIDBearer>, IComparable, IEquatable<IIDBearer>
	{
		string ID { get; }
		string Description { get; }
	}
}
