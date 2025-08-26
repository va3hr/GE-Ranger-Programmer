// MainForm.binary-tones.cs — minimal UI hook that uses RgrCodec
// This is *not* a full MainForm replacement. It shows the exact, minimal code
// to call the binary/nibble pipeline and write tones to the grid once.

using System;
using System.Windows.Forms;
using X2212;

public partial class MainForm : Form
{
    // Call this where you already have A3..B0 for the row.
    private void FillRowFromChannelBytes(DataGridViewCell txCell, DataGridViewCell rxCell,
                                         byte A3, byte A2, byte A1, byte A0,
                                         byte B3, byte B2, byte B1, byte B0)
    {
        ulong w = RgrCodec.PackNibbleWord(A3, A2, A1, A0, B3, B2, B1, B0);

        int txIndex = RgrCodec.BuildIndex(w, RgrCodec.TX_BITS);
        int rxIndex = RgrCodec.BuildIndex(w, RgrCodec.RX_BITS);

        if ((uint)txIndex >= (uint)RgrCodec.Cg.Length) txIndex = 0;
        if ((uint)rxIndex >= (uint)RgrCodec.Cg.Length) rxIndex = 0;

        txCell.Value = RgrCodec.Cg[txIndex];
        rxCell.Value = RgrCodec.Cg[rxIndex];
    }
}