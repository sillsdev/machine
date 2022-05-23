namespace SIL.Machine.Annotations
{
    public interface IAnnotatedData<TOffset>
    {
        Range<TOffset> Range { get; }
        AnnotationList<TOffset> Annotations { get; }
    }
}
