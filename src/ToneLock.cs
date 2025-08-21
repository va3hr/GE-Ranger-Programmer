// ToneLock.cs — RANGR6M2 tone decode/encode + X2212 packing
// Keeps all tone logic here. Namespace matches your repo.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RangrApp.Locked
{
    public static class ToneLock
    {
        // Canonical CG list (index -> string). Index 0 = "0".
        public static readonly string[] Cg = new[]
        {
            "0","67.0","71.9","74.4","77.0","79.7","82.5","85.4","88.5","91.5","94.8",
            "97.4","100.0","103.5","107.2","110.9","114.8","118.8","123.0","127.3",
            "131.8","136.5","141.3","146.2","151.4","156.7","162.2","167.9","173.8",
            "179.9","186.2","192.8","203.5","210.7"
        };

        // ---------------------------------------------------------------------
        // TX (RANGR6M2): Observed coding uses A1 (with one disambiguation on B1.bit7).
        // This covers the tones in your RANGR6M2 test image.
        // If you see a new A1 value later, just add a row here.
        // ---------------------------------------------------------------------

        // A1 -> CG index (B1.bit7 disambiguates 0x28 only)
        private static readonly D
