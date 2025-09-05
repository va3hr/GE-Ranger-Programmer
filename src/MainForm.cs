using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        private ToneDecoder _toneDecoder;
        private byte[] _processedFileData; // Stores the little-endian data for the UI

        public MainForm()
        {
            InitializeComponent();
            InitializeToneDecoder();
        }

        /// <summary>
        /// Initializes the ToneDecoder by loading the CSV rules.
        /// This is called once when the form is created.
        /// </summary>
        private void InitializeToneDecoder()
        {
            try
            {
                // Assumes the CSV is in the same directory as the .exe
                string toneCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToneCodes_TX_E8_ED_EE_EF.csv");
                if (!File.Exists(toneCsvPath))
                {
                    MessageBox.Show($"CRITICAL: Tone definition file not found.\nExpected at: {toneCsvPath}", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _toneDecoder = new ToneDecoder(toneCsvPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize the tone decoder: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the "Open File" button click.
        /// </summary>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (_toneDecoder == null)
            {
                MessageBox.Show("Tone Decoder is not initialized. Cannot process file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "RGR files (*.RGR)|*.RGR|All files (*.*)|*.*";
                openFileDialog.Title = "Open GE Ranger File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadAndProcessFile(openFileDialog.FileName);
                }
            }
        }

        /// <summary>
        /// Main logic to read a file, process its data, and update the UI.
        /// </summary>
        /// <param name="filePath">The full path to the .RGR file.</param>
        private void LoadAndProcessFile(string filePath)
        {
            try
            {
                byte[] rawFileData = File.ReadAllBytes(filePath);
                if (rawFileData.Length != 128)
                {
                    MessageBox.Show("Invalid file size. Expected 128 bytes.", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Convert from Big-Endian for processing
                _processedFileData = BigEndian.SwapBytes(rawFileData);

                // Clear UI before loading new data
                dgvChannels.Rows.Clear();
                
                // This is the radio's non-linear channel-to-memory mapping
                int[] channelAddresses = {
                    0xE0, 0xD0, 0xC0, 0xB0, 0xA0, 0x90, 0x80, 0x70,
                    0x60, 0x50, 0x40, 0x30, 0x20, 0x10, 0x00, 0xF0
                };
                
                for (int i = 0; i < 16; i++)
                {
                    int channelNumber = i + 1;
                    int baseAddress = channelAddresses[i];

                    // Get Frequencies (uses your existing working class)
                    string txFreq = FreqLock.GetFrequency(_processedFileData, baseAddress, true);
                    string rxFreq = FreqLock.GetFrequency(_processedFileData, baseAddress, false);

                    // Get Tones (uses the new, correct ToneDecoder class)
                    string txTone = _toneDecoder.GetTone(_processedFileData, baseAddress, true);
                    string rxTone = _toneDecoder.GetTone(_processedFileData, baseAddress, false);

                    dgvChannels.Rows.Add(channelNumber, txFreq, rxFreq, txTone, rxTone);
                }
                
                // Update the hex view on the side
                UpdateHexDump(_processedFileData);
                this.Text = $"GE Ranger Programmer - {Path.GetFileName(filePath)}"; // Update window title
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing file: {ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Formats and displays the 128 bytes of file data in the hex text box.
        /// </summary>
        private void UpdateHexDump(byte[] data)
        {
            if (data == null) return;

            var hexDump = new StringBuilder();
            for (int i = 0; i < data.Length; i += 16)
            {
                hexDump.Append($"{i:X4}: "); // Address
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length)
                    {
                        hexDump.Append($"{data[i + j]:X2} ");
                    }
                }
                hexDump.AppendLine();
            }
            txtHexView.Text = hexDump.ToString();
            txtHexView.SelectionStart = 0;
            txtHexView.ScrollToCaret();
        }

        // --- Placeholders for future functionality ---

        private void btnSaveFile_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Save functionality has not been implemented yet.");
        }

        private void btnWriteEeprom_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Write to EEPROM has not been implemented yet.");
        }
    }
}
