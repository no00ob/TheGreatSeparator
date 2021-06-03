using System;
using System.Collections.Generic;
using Dalamud.Game;

namespace TheGreatSeparator {
    internal static class Extensions {
        internal static bool TryScanText(this SigScanner scanner, string sig, out IntPtr result) {
            result = IntPtr.Zero;
            try {
                result = scanner.ScanText(sig);
                return true;
            } catch (KeyNotFoundException) {
                return false;
            }
        }

        internal static bool TryGetStaticAddressFromSig(this SigScanner scanner, string sig, out IntPtr result) {
            result = IntPtr.Zero;
            try {
                result = scanner.GetStaticAddressFromSig(sig);
                return true;
            } catch (KeyNotFoundException) {
                return false;
            }
        }
    }
}
