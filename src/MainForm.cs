using System;
using System.Drawing;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            // Main Form Setup
            this.Text = "GE Ranger X2212 Programmer";
            this.ClientSize = new Size(1200, 800);
            this.BackColor = SystemColors.Window;
            
            // Top Menu
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var deviceMenu = new ToolStripMenuItem("Device");
            menuStrip.Items.AddRange(new[] { fileMenu, deviceMenu });
            this.Controls.Add(menuStrip);
            
            // Channel Grid - Only showing Channel, TX, RX, and Tone columns
            var grid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(1160, 700),
                ColumnCount = 5,
                Columns = {
                    new DataGridViewTextBoxColumn { Name = "Ch", HeaderText = "Ch", Width = 50 },
                    new DataGridViewTextBoxColumn { Name = "TxFreq", HeaderText = "Tx Freq", Width = 120 },
                    new DataGridViewTextBoxColumn { Name = "RxFreq", HeaderText = "Rx Freq", Width = 120 },
                    new DataGridViewComboBoxColumn { Name = "TxTone", HeaderText = "Tx Tone", Width = 150 },
                    new DataGridViewComboBoxColumn { Name = "RxTone", HeaderText = "Rx Tone", Width = 150 }
                },
                RowCount = 16,
                AllowUserToAddRows = false,
                ReadOnly = true
            };
            
            // Populate channel numbers
            for (int i = 0; i < 16; i++)
            {
                grid.Rows[i].Cells["Ch"].Value = i + 1;
            }
            
            this.Controls.Add(grid);
            
            // Status Label
            var statusLabel = new Label
            {
                Text = "Ready to program X2212",
                Location = new Point(10, 750),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(statusLabel);
        }
    }
}
