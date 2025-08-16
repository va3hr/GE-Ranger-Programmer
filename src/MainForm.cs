using System;
using System.Drawing;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            // Initialize in thread-safe way
            if (InvokeRequired)
            {
                Invoke(new Action(() => InitializeComponent()));
            }
            else
            {
                InitializeComponent();
            }
        }

        private void InitializeComponent()
        {
            // ... [your existing UI code] ...
        }
    }
}
