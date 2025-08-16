namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            // Initialize components manually
            this.SuspendLayout();
            
            // Add basic controls
            this.Text = "GE Ranger Programmer";
            this.ClientSize = new Size(800, 600);
            
            var statusLabel = new Label
            {
                Text = "Ready to program X2212",
                Location = new Point(10, 10),
                AutoSize = true
            };
            this.Controls.Add(statusLabel);
            
            this.ResumeLayout(false);
        }
    }
}
