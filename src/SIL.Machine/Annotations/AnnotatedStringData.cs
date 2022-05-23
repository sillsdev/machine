using System.Collections.Generic;
using System.Text;
using SIL.Machine.DataStructures;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
    public class AnnotatedStringData : IAnnotatedData<int>, ICloneable<AnnotatedStringData>
    {
        private readonly StringBuilder _str;

        public AnnotatedStringData(string str)
        {
            _str = new StringBuilder(str);
            Range = Range<int>.Create(0, _str.Length);
            Annotations = new AnnotationList<int>();
        }

        protected AnnotatedStringData(AnnotatedStringData sd)
        {
            _str = new StringBuilder(sd._str.ToString());
            Range = sd.Range;
            Annotations = sd.Annotations.Clone();
        }

        public string String
        {
            get { return _str.ToString(); }
        }

        public Range<int> Range { get; private set; }

        public AnnotationList<int> Annotations { get; }

        public void Replace(int index, int length, string str)
        {
            if (length == str.Length)
            {
                _str.Remove(index, length);
                _str.Insert(index, str);
            }
            else
            {
                Remove(index, length);
                Insert(index, str);
            }
        }

        public void Insert(int index, string str)
        {
            _str.Insert(index, str);
            foreach (Annotation<int> ann in Annotations)
            {
                foreach (Annotation<int> a in ann.GetNodesDepthFirst())
                {
                    if (a.Range.Start >= index)
                        a.Range = Range<int>.Create(a.Range.Start + str.Length, a.Range.End + str.Length);
                    else if (a.Range.End > index)
                        a.Range = Range<int>.Create(a.Range.Start, a.Range.End + str.Length);
                }
            }
            Range = Range<int>.Create(0, _str.Length);
        }

        public void Remove(int index, int length)
        {
            Range<int> removedRange = Range<int>.Create(index, index + length);
            _str.Remove(index, length);
            var toRemove = new List<Annotation<int>>();
            foreach (Annotation<int> ann in Annotations)
            {
                foreach (Annotation<int> a in ann.GetNodesDepthFirst())
                {
                    if (removedRange.Contains(a.Range))
                        toRemove.Add(a);
                    else if (a.Range.Start >= index)
                        a.Range = Range<int>.Create(a.Range.Start - length, a.Range.End - length);
                    else if (a.Range.End > index)
                        a.Range = Range<int>.Create(a.Range.Start, a.Range.End - length);
                }
            }

            foreach (Annotation<int> ann in toRemove)
            {
                if (ann.List != null)
                    ann.Remove(false);
            }
            Range = Range<int>.Create(0, _str.Length);
        }

        public AnnotatedStringData Clone()
        {
            return new AnnotatedStringData(this);
        }

        public override string ToString()
        {
            return _str.ToString();
        }
    }
}
