namespace SIL.APRE
{
	public interface IData<TOffset>
	{
		Span<TOffset> Span { get; }
		AnnotationList<TOffset> Annotations { get; } 
	}
}
