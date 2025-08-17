using System;
using System.Drawing;
using System.IO;
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
            this.MinimumSize = new Size(1000, 700);
            this.Size = new Size(1200, 800);
            this.Text = "GE Ranger X2212 Programmer";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Control;
            
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
            
            // Channel Grid
            grid = new DataGridView();
            grid.Location = new Point(20, 40);
            grid.Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 100);
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
            
            // Add default rows
            for (int i = 0; i < 16; i++)
            {
                grid.Rows.Add();
                grid.Rows[i].Cells[0].Value = i + 1;
                grid.Rows[i].Cells[1].Value = "0.000";
                grid.Rows[i].Cells[2].Value = "0.000";
                grid.Rows[i].Cells[3].Value = "Off";
                grid.Rows[i].Cells[4].Value = "Off";
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
                    try
                    {
                        var lines = File.ReadAllLines(dialog.FileName);
                        grid.Rows.Clear();
                        
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            
                            var parts = line.Split('|');
                            if (parts.Length >= 5)
                            {
                                int rowIndex = grid.Rows.Add();
                                grid.Rows[rowIndex].Cells[0].Value = parts[0].Trim();
                                grid.Rows[rowIndex].Cells[1].Value = parts[1].Trim();
                                grid.Rows[rowIndex].Cells[2].Value = parts[2].Trim();
                                grid.Rows[rowIndex].Cells[3].Value = parts[3].Trim();
                                grid.Rows[rowIndex].Cells[4].Value = parts[4].Trim();
                            }
                        }
                        statusLabel.Text = $"Loaded: {Path.GetFileName(dialog.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file:\n{ex.Message}", 
                                      "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveFileHandler(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Ranger Files (*.rgr)|*.rgr";
                dialog.DefaultExt = "rgr";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var writer = new StreamWriter(dialog.FileName))
                        {
                            foreach (DataGridViewRow row in grid.Rows)
                            {
                                if (!row.IsNewRow)
                                {
                                    writer.WriteLine(
                                        $"{row.Cells[0].Value}|{row.Cells[1].Value}|" +
                                        $"{row.Cells[2].Value}|{row.Cells[3].Value}|" +
                                        $"{row.Cells[4].Value}");
                                }
                            }
                        }
                        statusLabel.Text = $"Saved: {Path.GetFileName(dialog.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file:\n{ex.Message}", 
                                      "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ReadAllHandler(object sender, EventArgs e)
        {
            statusLabel.Text = "Reading from X2212...";
            // TODO: Implement actual device reading
            MessageBox.Show("Read All functionality will be implemented next", 
                          "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = "Read simulated - implement hardware access";
        }
    }
}
