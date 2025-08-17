using System;
using System.Drawing;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public class MainForm : Form
    {
        private DataGridView grid;
        private Label statusLabel;

        public MainForm()
        {
            // Initialize with proper size
            this.MinimumSize = new Size(800, 600);
            this.Size = new Size(1000, 700);
            this.Text = "GE Ranger X2212 Programmer";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Control; // Match background
            
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
            fileMenu.DropDownItems.Add("Open", null, OpenFileHandler);
            fileMenu.DropDownItems.Add("Save", null, SaveFileHandler);
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Application.Exit());
            deviceMenu.DropDownItems.Add("Read All", null, ReadAllHandler);
            
            // Calculate grid size based on form
            int gridWidth = this.ClientSize.Width - 40;
            int gridHeight = this.ClientSize.Height - 100;
            
            // Channel Grid
            grid = new DataGridView();
            grid.Location = new Point(20, 40);
            grid.Size = new Size(gridWidth, gridHeight);
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | 
                          AnchorStyles.Left | AnchorStyles.Right;
            grid.ColumnCount = 5;
            grid.Columns[0].Name = "Ch";
            grid.Columns[1].Name = "Tx Freq";
            grid.Columns[2].Name = "Rx Freq";
            grid.Columns[3].Name = "Tx Tone";
            grid.Columns[4].Name = "Rx Tone";
            grid.Columns[0].Width = 40;
            grid.Columns[1].Width = 100;
            grid.Columns[2].Width = 100;
            grid.Columns[3].Width = 120;
            grid.Columns[4].Width = 120;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            
            // Add rows
            for (int i = 0; i < 16; i++)
            {
                grid.Rows.Add();
                grid.Rows[i].Cells[0].Value = i + 1;
            }
            
            this.Controls.Add(grid);
            
            // Status Label
            statusLabel = new Label();
            statusLabel.Text = "Ready to program X2212";
            statusLabel.Location = new Point(20, this.ClientSize.Height - 30);
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLabel.AutoSize = true;
            this.Controls.Add(statusLabel);
        }

        private void OpenFileHandler(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Ranger Files (*.rgr)|*.rgr|All files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    statusLabel.Text = $"Opened: {dialog.FileName}";
                    // Add your file loading logic here
                }
            }
        }

        private void SaveFileHandler(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Ranger Files (*.rgr)|*.rgr|All files (*.*)|*.*";
                dialog.DefaultExt = "rgr";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    statusLabel.Text = $"Saved: {dialog.FileName}";
                    // Add your file saving logic here
                }
            }
        }

        private void ReadAllHandler(object sender, EventArgs e)
        {
            statusLabel.Text = "Reading from X2212...";
            // Add your read-from-device logic here
            
            // Simulate read completion
            System.Threading.Tasks.Task.Delay(2000).ContinueWith(t => {
                this.Invoke((MethodInvoker)delegate {
                    statusLabel.Text = "Read completed!";
                });
            });
        }
    }
}
