using System.Collections.Generic;
using System.Text;
using SIL.Collections;

namespace SIL.Machine
{
	public class StringData : IData<int>, IDeepCloneable<StringData>
	{
		private readonly SpanFactory<int> _spanFactory;
		private readonly StringBuilder _str;
		private Span<int> _span; 
		private readonly AnnotationList<int> _annotations; 

		public StringData(SpanFactory<int> spanFactory, string str)
		{
			_spanFactory = spanFactory;
			_str = new StringBuilder(str);
			_span = _spanFactory.Create(0, _str.Length);
			_annotations = new AnnotationList<int>(spanFactory);
		}

		protected StringData(StringData sd)
		{
			_spanFactory = sd._spanFactory;
			_str = new StringBuilder(sd._str.ToString());
			_span = sd._span;
			_annotations = sd._annotations.DeepClone();
		}

		public string String
		{
			get { return _str.ToString(); }
		}

		public Span<int> Span
		{
			get { return _span; }
		}

		public AnnotationList<int> Annotations
		{
			get { return _annotations; }
		}

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
			foreach (Annotation<int> ann in _annotations)
			{
				foreach (Annotation<int> a in ann.GetNodesDepthFirst())
				{
					if (a.Span.Start >= index)
						a.Span = _spanFactory.Create(a.Span.Start + str.Length, a.Span.End + str.Length);
					else if (a.Span.End > index)
						a.Span = _spanFactory.Create(a.Span.Start, a.Span.End + str.Length);
				}
			}
			_span = _spanFactory.Create(0, _str.Length);
		}

		public void Remove(int index, int length)
		{
			Span<int> removedSpan = _spanFactory.Create(index, index + length);
			_str.Remove(index, length);
			var toRemove = new List<Annotation<int>>();
			foreach (Annotation<int> ann in _annotations)
			{
				foreach (Annotation<int> a in ann.GetNodesDepthFirst())
				{
					if (removedSpan.Contains(a.Span))
						toRemove.Add(a);
					else if (a.Span.Start >= index)
						a.Span = _spanFactory.Create(a.Span.Start - length, a.Span.End - length);
					else if (a.Span.End > index)
						a.Span = _spanFactory.Create(a.Span.Start, a.Span.End - length);
				}
			}

			foreach (Annotation<int> ann in toRemove)
			{
				if (ann.List != null)
					ann.Remove(false);
			}
			_span = _spanFactory.Create(0, _str.Length);
		}

		public StringData DeepClone()
		{
			return new StringData(this);
		}
			
		public override string ToString()
		{
			return _str.ToString();
		}
	}
}
