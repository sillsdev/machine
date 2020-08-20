using System;
using Eto;
using Eto.Drawing;
using Eto.Forms;

namespace SIL.Machine.Translation.TestApp.Wpf
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			Style.Add<TextArea>(null, c => c.Font = new Font(c.Font.Family, 12));
			new Application(Platforms.Wpf).Run(new MainForm());
		}
	}
}
