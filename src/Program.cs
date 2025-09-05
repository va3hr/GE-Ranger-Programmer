using System;
using System.Windows.Forms;
namespace GE_Ranger_Programmer
{
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
}
