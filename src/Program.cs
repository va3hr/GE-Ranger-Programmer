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
                var mainForm = new MainForm();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error: {ex.Message}\n\n{ex.StackTrace}", 
                              "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
