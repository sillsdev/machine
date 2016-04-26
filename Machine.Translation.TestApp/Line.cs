using System;
using Eto.Drawing;
using Eto.Forms;

namespace SIL.Machine.Translation.TestApp
{
	public class Line : Drawable
	{
		private readonly Pen _pen;

		public Line()
		{
			_pen = new Pen(Colors.Black);
			Height = 1;
		}

		public Color Color
		{
			get { return _pen.Color; }
			set
			{
				if (_pen.Color != value)
				{
					_pen.Color = value;
					Invalidate();
				}
			}
		}

		public float Thickness
		{
			get { return _pen.Thickness; }
			set
			{
				if (Math.Abs(_pen.Thickness - value) > float.Epsilon)
				{
					_pen.Thickness = value;
					Height = (int) value;
					Invalidate();
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.DrawLine(_pen, e.ClipRectangle.MiddleLeft, e.ClipRectangle.MiddleRight);
			base.OnPaint(e);
		}
	}
}
