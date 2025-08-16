using System;
using System.Drawing;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            // Initialize with proper size
            this.MinimumSize = new Size(1000, 700);
            this.Size = new Size(1200, 800);
            this.Text = "GE Ranger X2212 Programmer";
            this.StartPosition = FormStartPosition.CenterScreen;
            
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Top Menu
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var deviceMenu = new ToolStripMenuItem("Device");
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, deviceMenu });
            this.Controls.Add(menuStrip);
            
            // Add menu items
            fileMenu.DropDownItems.Add("Open", null, (s,e) => { /* Handle open */ });
            fileMenu.DropDownItems.Add("Save", null, (s,e) => { /* Handle save */ });
            fileMenu.DropDownItems.Add("Exit", null, (s,e) => Application.Exit());
            deviceMenu.DropDownItems.Add("Read All", null, (s,e) => { /* Handle read */ });
            
            // Channel Grid
            var grid = new DataGridView();
            grid.Location = new Point(10, 40);
            grid.Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 100);
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grid.ColumnCount = 5;
            grid.Columns.Add("Ch", "Ch");
            grid.Columns.Add("TxFreq", "Tx Freq");
            grid.Columns.Add("RxFreq", "Rx Freq");
            grid.Columns.Add("TxTone", "Tx Tone");
            grid.Columns.Add("RxTone", "Rx Tone");
            grid.Columns["Ch"].Width = 40;
            grid.Columns["TxFreq"].Width = 100;
            grid.Columns["RxFreq"].Width = 100;
            grid.Columns["TxTone"].Width = 120;
            grid.Columns["RxTone"].Width = 120;
            grid.AllowUserToAddRows = false;
            
            // Add rows safely
            for (int i = 0; i < 16; i++)
            {
                grid.Rows.Add();
                grid.Rows[i].Cells["Ch"].Value = i + 1;
            }
            
            this.Controls.Add(grid);
            
            // Status Label
            var statusLabel = new Label();
            statusLabel.Text = "Ready to program X2212";
            statusLabel.Location = new Point(10, this.ClientSize.Height - 30);
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLabel.AutoSize = true;
            this.Controls.Add(statusLabel);
        }
    }
}
