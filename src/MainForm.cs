using System;
using System.Drawing;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        // Core data fields - ADD ONLY the missing ones your code needs
        private ushort _lptBaseAddress = 0xA800;
        private byte[] _currentData = new byte[128];
        private string _lastFilePath = "";
        private bool _dataModified = false;
        private byte[] _clipboardRow = new byte[8];
        private byte[] _undoData = new byte[128]; // Missing field
        private int _currentChannel = 1; // Missing field  
        private int _lastSelectedRow = 0; // Missing field

        // UI Controls - keep your existing declarations
        private MenuStrip menuStrip = null!;
        private ToolStripMenuItem fileMenu = null!;
        private ToolStripMenuItem editMenu = null!;
        private ToolStripMenuItem deviceMenu = null!;
        private Panel topPanel = null!;
        private Label lblLptBase = null!;
        private TextBox txtLptBase = null!;
        private Label lblDevice = null!;
        private TextBox txtDevice = null!;
        private Label lblChannel = null!;
        private TextBox txtChannel = null!;
        private DataGridView hexGrid = null!;
        private TextBox txtMessages = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel statusLabel = null!;

        public MainForm()
        {
            InitializeComponent();
            // Call LoadSettings and other methods AFTER the form is shown
            this.Shown += MainForm_Shown;
        }

        private void MainForm_Shown(object? sender, EventArgs e)
        {
            LoadSettings(); // Now this will work because partial classes are loaded
            InitializeSafety();
            UpdateChannelDisplay();
        }

        // Keep your exact InitializeComponent and CreateXXX methods as they were working
        // (Don't change the hex grid structure!)

        // Only add these simple utility methods that don't conflict
        private void UpdateChannelDisplay()
        {
            if (txtChannel != null)
            {
                txtChannel.Text = $"Ch{_currentChannel}";
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
        }

        private void InitializeSafety()
        {
            LogMessage("X2212 Programmer initialized");
            LogMessage($"LPT Base Address: 0x{_lptBaseAddress:X4}");
            LogMessage("Ready for device operations");
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
