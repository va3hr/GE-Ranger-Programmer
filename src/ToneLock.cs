// SPDX-License-Identifier: MIT
// Compatibility wrapper so existing code that references ToneLock keeps compiling.
#nullable enable
using System;
using System.Collections.Generic;

namespace RangrApp.Locked;

public static class ToneLock
{
    // Legacy surface
    public static readonly string[] MenuAll = TxToneLock.MenuAll;
    public static bool TryDisplayToIndex(string? display, out byte index)
        => TxToneLock.TryDisplayToIndex(display, out index);
    public static string IndexToDisplay(byte index)
        => TxToneLock.IndexToDisplay(index);

    // Preferred explicit surfaces
    public static IReadOnlyList<string> ToneMenuTx => TxToneLock.ToneMenu;
    public static IReadOnlyList<string> ToneMenuRx => RxToneLock.ToneMenu;

    // RX helpers used by UI/codec
    public static string DecodeRxFromA3B3(byte a3, byte b3, string txDisplay)
        => RxToneCodec.DecodeRxTone(a3, b3, txDisplay);

    public static void EncodeRxToA3B3(ref byte a3, ref byte b3, string display, bool follow, int bank)
    {
        var (na3, nb3) = RxToneCodec.EncodeRxTone(a3, b3, display, follow, bank);
        a3 = na3; b3 = nb3;
    }
}
