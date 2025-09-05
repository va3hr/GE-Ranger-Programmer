using System;
using System.Windows.Forms;


namespace RangrApp.Locked // Your project's root namespace can stay the same
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

