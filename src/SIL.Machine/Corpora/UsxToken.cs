using System.Xml.Linq;

namespace SIL.Machine.Corpora
{
    public class UsxToken
    {
        public UsxToken(XElement paraElem, string text, XElement elem = null)
        {
            ParaElement = paraElem;
            Text = text;
            Element = elem;
        }

        public XElement ParaElement { get; }
        public string Text { get; }
        public XElement Element { get; }

        public override string ToString()
        {
            return Text;
        }
    }
}
