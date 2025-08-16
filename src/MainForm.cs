using System;
using System.Drawing;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
        public MainForm()
{
    // Set minimum size first
    this.MinimumSize = new Size(1000, 700);
    
    // Then set actual size
    this.Size = new Size(1200, 800);
    
    // Rest of initialization...
}
            // Initialize with proper size
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);
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
            
            // Channel Grid - SAFE INITIALIZATION
            var grid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 5,
                Columns = {
                    new DataGridViewTextBoxColumn { Name = "Ch", HeaderText = "Ch", Width = 40 },
                    new DataGridViewTextBoxColumn { Name = "TxFreq", HeaderText = "Tx Freq", Width = 100 },
                    new DataGridViewTextBoxColumn { Name = "RxFreq", HeaderText = "Rx Freq", Width = 100 },
                    new DataGridViewComboBoxColumn { Name = "TxTone", HeaderText = "Tx Tone", Width = 120 },
                    new DataGridViewComboBoxColumn { Name = "RxTone", HeaderText = "Rx Tone", Width = 120 }
                },
                AllowUserToAddRows = false
            };
            
            // SAFE: Add rows one by one
            for (int i = 0; i < 16; i++)
            {
                grid.Rows.Add(); // Create new row first
                grid.Rows[i].Cells["Ch"].Value = i + 1;
            }
            
            this.Controls.Add(grid);
            
            // Status Label
            var statusLabel = new Label
            {
                Text = "Ready to program X2212",
                Location = new Point(10, this.ClientSize.Height - 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                AutoSize = true
            };
            this.Controls.Add(statusLabel);
        }
    }
}
