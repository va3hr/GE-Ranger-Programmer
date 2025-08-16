using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GE_Ranger_Programmer
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [STAThread]
        static void Main()
        {
            AllocConsole(); // Show console for debugging
            Console.WriteLine("Starting application...");

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRASH: {ex}");
                MessageBox.Show($"Fatal error: {ex.Message}\n\nCheck console for details", 
                              "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
