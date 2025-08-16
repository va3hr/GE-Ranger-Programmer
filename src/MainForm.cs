using System;
using System.Drawing;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public class MainForm : Form
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
            // 1. Menu Strip
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var deviceMenu = new ToolStripMenuItem("Device");
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(deviceMenu);
            this.Controls.Add(menuStrip);
            
            // Add menu items
            fileMenu.DropDownItems.Add("Open", null, (s,e) => { /* Handle open */ });
            fileMenu.DropDownItems.Add("Save", null, (s,e) => { /* Handle save */ });
            fileMenu.DropDownItems.Add("Exit", null, (s,e) => Application.Exit());
            deviceMenu.DropDownItems.Add("Read All", null, (s,e) => { /* Handle read */ });
            
            // 2. Channel Grid (FIXED: No extra columns)
            var grid = new DataGridView();
            grid.Location = new Point(10, menuStrip.Height + 10);
            grid.Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - menuStrip.Height - 50);
            
            // Configure columns
            grid.ColumnCount = 5;
            grid.Columns[0].Name = "Ch";
            grid.Columns[1].Name = "Tx Freq";
            grid.Columns[2].Name = "Rx Freq";
            grid.Columns[3].Name = "Tx Tone";
            grid.Columns[4].Name = "Rx Tone";
            
            // Set widths
            grid.Columns[0].Width = 40;
            grid.Columns[1].Width = 100;
            grid.Columns[2].Width = 100;
            grid.Columns[3].Width = 120;
            grid.Columns[4].Width = 120;
            
            // Remove extra columns
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AllowUserToAddRows = false;
            
            // Add rows
            for (int i = 0; i < 16; i++)
            {
                grid.Rows.Add();
                grid.Rows[i].Cells[0].Value = i + 1;
            }
            
            this.Controls.Add(grid);
            
            // 3. Status Label
            var statusLabel = new Label();
            statusLabel.Text = "Ready to program X2212";
            statusLabel.Location = new Point(10, this.ClientSize.Height - 30);
            statusLabel.AutoSize = true;
            this.Controls.Add(statusLabel);
        }
    }
}
