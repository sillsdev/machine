namespace SIL.Collections
{
	public interface IFreezable
	{
		bool IsFrozen { get; }
		void Freeze();
	}
}
