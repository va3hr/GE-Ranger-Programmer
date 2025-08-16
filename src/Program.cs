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
                MessageBox.Show($"Fatal application error: {ex.Message}\n\n{ex.StackTrace}", 
                              "Critical Failure", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Error);
            }
        }
    }
}
