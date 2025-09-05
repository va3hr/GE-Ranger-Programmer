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

        private void InitializeToneDecoder()
        {
            try
            {
                string toneCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToneCodes_TX_E8_ED_EE_EF.csv");
                _toneDecoder = new ToneDecoder(toneCsvPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tone file: {ex.Message}", "Error");
            }
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (_toneDecoder == null) return;
            using (var openFileDialog = new OpenFileDialog { Filter = "RGR files (*.RGR)|*.RGR" })
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
                _processedFileData = BigEndian.SwapBytes(rawFileData);

                dgvChannels.Rows.Clear();
                int[] channelAddresses = { 0xE0, 0xD0, 0xC0, 0xB0, 0xA0, 0x90, 0x80, 0x70, 0x60, 0x50, 0x40, 0x30, 0x20, 0x10, 0x00, 0xF0 };

                for (int i = 0; i < 16; i++)
                {
                    int baseAddress = channelAddresses[i];
                    string txFreq = FreqLock.GetTxFreq(_processedFileData, baseAddress);
                    string rxFreq = FreqLock.GetRxFreq(_processedFileData, baseAddress);
                    string txTone = _toneDecoder.GetTone(_processedFileData, baseAddress, true);
                    string rxTone = _toneDecoder.GetTone(_processedFileData, baseAddress, false);
                    dgvChannels.Rows.Add(i + 1, txFreq, rxFreq, txTone, rxTone);
                }
                UpdateHexDump(_processedFileData);
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
