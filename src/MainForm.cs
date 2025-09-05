using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    public partial class MainForm : Form
    {
        private ToneDecoder? _toneDecoder;
        private byte[]? _processedFileData;

        public MainForm()
        {
            InitializeComponent();
            InitializeToneDecoder();
        }

        /// <summary>
        /// Initializes the ToneDecoder, which has the tone data built-in.
        /// </summary>
        private void InitializeToneDecoder()
        {
            _toneDecoder = new ToneDecoder();
        }

        /// <summary>
        /// This is your original byte-swapping logic, restored.
        /// It is used to convert the file's word order.
        /// </summary>
        private byte[] SwapBytes(byte[] data)
        {
            byte[] swapped = new byte[data.Length];
            for (int i = 0; i < data.Length; i += 2)
            {
                if (i + 1 < data.Length)
                {
                    swapped[i] = data[i + 1];
                    swapped[i + 1] = data[i];
                }
            }
            return swapped;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (_toneDecoder == null)
            {
                 MessageBox.Show("Tone Decoder failed to initialize.", "Error");
                 return;
            }

            using (var openFileDialog = new OpenFileDialog { Filter = "RGR files (*.RGR)|*.RGR|All files (*.*)|*.*" })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadAndProcessFile(openFileDialog.FileName);
                }
            }
        }
        
        private void LoadAndProcessFile(string filePath)
        {
            try
            {
                byte[] rawFileData = File.ReadAllBytes(filePath);
                if (rawFileData.Length != 128) 
                {
                    MessageBox.Show("Invalid file size. Expected 128 bytes.", "File Error");
                    return;
                }

                // This calls your original, restored SwapBytes method.
                _processedFileData = SwapBytes(rawFileData);

                dgvChannels.Rows.Clear();
                int[] channelAddresses = { 0xE0, 0xD0, 0xC0, 0xB0, 0xA0, 0x90, 0x80, 0x70, 0x60, 0x50, 0x40, 0x30, 0x20, 0x10, 0x00, 0xF0 };

                for (int i = 0; i < 16; i++)
                {
                    int baseAddress = channelAddresses[i];
                    // Calls your separate, working FreqLock class
                    string txFreq = FreqLock.GetTxFreq(_processedFileData, baseAddress);
                    string rxFreq = FreqLock.GetRxFreq(_processedFileData, baseAddress);
                    
                    // Calls the new, self-contained ToneDecoder class
                    string txTone = _toneDecoder.GetTone(_processedFileData, baseAddress, true);
                    string rxTone = _toneDecoder.GetTone(_processedFileData, baseAddress, false);
                    
                    dgvChannels.Rows.Add(i + 1, txFreq, rxFreq, txTone, rxTone);
                }
                UpdateHexDump(_processedFileData);
                this.Text = $"GE Ranger Programmer - {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing file: {ex.Message}", "Processing Error");
            }
        }

        private void UpdateHexDump(byte[] data)
        {
            var hexDump = new StringBuilder();
            for (int i = 0; i < data.Length; i += 16)
            {
                hexDump.Append($"{i:X4}: ");
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length) hexDump.Append($"{data[i + j]:X2} ");
                }
                hexDump.AppendLine();
            }
            txtHexView.Text = hexDump.ToString();
        }
    }
}
