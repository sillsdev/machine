using System;
using Eto;
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
			new Application(Platform.Detect).Run(new MainForm());
		}
	}
}
