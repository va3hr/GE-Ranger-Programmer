using System;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: {ex.Message}", "Application Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
