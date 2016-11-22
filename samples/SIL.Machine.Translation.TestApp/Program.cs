using System;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using GalaSoft.MvvmLight.Threading;

namespace SIL.Machine.Translation.TestApp
{
	public class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			DispatcherHelper.Initialize();
			Style.Add<TextArea>(null, c => c.Font = new Font(c.Font.Family, 12));
			new Application(Platform.Detect).Run(new MainForm());
		}
	}
}
