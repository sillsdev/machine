using System.Collections.Generic;
using System.Text;
using SIL.Machine.DataStructures;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
	public class AnnotatedStringData : IAnnotatedData<int>, ICloneable<AnnotatedStringData>
	{
		private readonly StringBuilder _str;
		private Span<int> _span; 
		private readonly AnnotationList<int> _annotations; 

		public AnnotatedStringData(string str)
		{
			_str = new StringBuilder(str);
			_span = Span<int>.Create(0, _str.Length);
			_annotations = new AnnotationList<int>();
		}

		protected AnnotatedStringData(AnnotatedStringData sd)
		{
			_str = new StringBuilder(sd._str.ToString());
			_span = sd._span;
			_annotations = sd._annotations.Clone();
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
						a.Span = Span<int>.Create(a.Span.Start + str.Length, a.Span.End + str.Length);
					else if (a.Span.End > index)
						a.Span = Span<int>.Create(a.Span.Start, a.Span.End + str.Length);
				}
			}
			_span = Span<int>.Create(0, _str.Length);
		}

		public void Remove(int index, int length)
		{
			Span<int> removedSpan = Span<int>.Create(index, index + length);
			_str.Remove(index, length);
			var toRemove = new List<Annotation<int>>();
			foreach (Annotation<int> ann in _annotations)
			{
				foreach (Annotation<int> a in ann.GetNodesDepthFirst())
				{
					if (removedSpan.Contains(a.Span))
						toRemove.Add(a);
					else if (a.Span.Start >= index)
						a.Span = Span<int>.Create(a.Span.Start - length, a.Span.End - length);
					else if (a.Span.End > index)
						a.Span = Span<int>.Create(a.Span.Start, a.Span.End - length);
				}
			}

			foreach (Annotation<int> ann in toRemove)
			{
				if (ann.List != null)
					ann.Remove(false);
			}
			_span = Span<int>.Create(0, _str.Length);
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
