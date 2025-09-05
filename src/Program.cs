using System;
using System.Windows.Forms;
using RangrApp.Locked; // <-- Add this using directive

namespace GE_Ranger_Programmer // This can be your project's default namespace
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
