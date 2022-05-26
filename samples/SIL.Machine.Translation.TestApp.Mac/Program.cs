using System;
using Eto;
using Eto.Drawing;
using Eto.Forms;

namespace SIL.Machine.Translation.TestApp.Mac
{
    class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Style.Add<TextArea>(null, c => c.Font = new Font(c.Font.Family, 12));
            new Application(Platforms.Mac64).Run(new MainForm());
        }
    }
}
