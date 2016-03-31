using System;
using Eto;
using Eto.Forms;

namespace SIL.Machine.Translation.TestApp
{
	public class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Platform.Detect).Run(new MainForm());
		}
	}
}
