namespace SIL.Machine.Translation.TestApp
{
    public class Segment
    {
        public Segment()
        {
            Text = string.Empty;
        }

        public string Text { get; set; }

        public int StartIndex { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
