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

		public float Thickness
		{
			get => _pen.Thickness;
			set
			{
				if (Math.Abs(_pen.Thickness - value) > float.Epsilon)
				{
					_pen.Thickness = value;
					Height = (int)value;
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
