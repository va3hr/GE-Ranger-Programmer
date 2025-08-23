// SPDX-License-Identifier: MIT
// Compatibility wrapper to preserve existing references to ToneLock while TX/RX are split.
// Drop this in alongside TxToneLock.cs, RxToneLock.cs, RxToneCodec.cs.
// No namespace version to avoid import issues.

#nullable enable
using System;

public static class ToneLock
{
    // Legacy surface (kept for older call sites)
    // Historically, MenuAll was shared. We map it to TX for dropdown population.
    public static readonly string[] MenuAll = TxToneLock.MenuAll;

    public static bool TryDisplayToIndex(string? display, out byte index)
        => TxToneLock.TryDisplayToIndex(display, out index);

    public static string IndexToDisplay(byte index)
        => TxToneLock.IndexToDisplay(index);

    // New explicit surfaces (preferred going forward)
    public static readonly string[] TxMenu = TxToneLock.MenuAll;
    public static readonly string[] RxMenu = RxToneLock.MenuAll;

    public static bool TryTxDisplayToIndex(string? display, out byte index)
        => TxToneLock.TryDisplayToIndex(display, out index);

    public static bool TryRxDisplayToIndex(string? display, out byte index)
        => RxToneLock.TryDisplayToIndex(display, out index);

    public static string TxIndexToDisplay(byte index)
        => TxToneLock.IndexToDisplay(index);

    public static string RxIndexToDisplay(byte index)
        => RxToneLock.IndexToDisplay(index);
}
