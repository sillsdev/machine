namespace SIL.Collections
{
	public interface IDeepCloneable<out T>
	{
		T DeepClone();
	}
}
