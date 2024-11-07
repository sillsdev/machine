using System.Xml.Linq;

namespace SIL.Machine.Corpora
{
    public class UsxToken
    {
        public UsxToken(XElement parentElement, string text, XElement elem = null)
        {
            ParentElement = parentElement;
            Text = text;
            Element = elem;
        }

        public XElement ParentElement { get; }
        public string Text { get; }
        public XElement Element { get; }

        public override string ToString()
        {
            return Text;
        }
    }
}
