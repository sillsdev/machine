namespace SIL.Machine.Annotations
{
	public interface IAnnotatedData<TOffset>
	{
		Span<TOffset> Span { get; }
		AnnotationList<TOffset> Annotations { get; } 
	}
}
