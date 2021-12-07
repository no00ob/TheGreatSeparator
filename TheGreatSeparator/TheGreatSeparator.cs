using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TheGreatSeparator {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TheGreatSeparator : IDalamudPlugin {
        public string Name => "The Great Separator";

        private static class Signatures {
            internal const string ShowFlyText = "E8 ?? ?? ?? ?? FF C7 41 D1 C7";
            internal const string SprintfNumber = "48 83 EC 28 44 8B C9";

            internal const string FlyTextStringify = "45 33 C0 C6 44 24 ?? ?? 41 8B D6 E8";
            internal const string HotbarManaStringify = "45 33 C0 48 8B CE C6 44 24 ?? ?? 42 8B 14 38 E8";
            internal const string PartyListStringify = "45 33 C0 C6 44 24 ?? ?? 8B D5 E8 ?? ?? ?? ?? EB";

            internal const string Separator = "44 0F B6 05 ?? ?? ?? ?? 45 84 C0 74 36 F6 87";
        }

        private static readonly byte[] ThirdArgOne = {
            0x41, 0xB0, 0x01,
        };

        private delegate void ShowFlyTextDelegate(IntPtr addon, uint actorIndex, uint messageMax, IntPtr numbers, int offsetNum, int offsetNumMax, IntPtr strings, int offsetStr, int offsetStrMax, int a10);

        private Hook<ShowFlyTextDelegate>? ShowFlyTextHook { get; }

        private delegate IntPtr SprintfNumberDelegate(uint number);

        private Hook<SprintfNumberDelegate>? SprintfNumberHook { get; }

        [PluginService]
        internal DalamudPluginInterface Interface { get; init; } = null!;

        [PluginService]
        internal CommandManager CommandManager { get; init; } = null!;

        [PluginService]
        internal SigScanner SigScanner { get; init; } = null!;

        internal Configuration Config { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }
        private Dictionary<IntPtr, byte[]> OldBytes { get; } = new();
        private byte OriginalSeparator { get; }
        private IntPtr SeparatorPtr { get; } = IntPtr.Zero;

        public TheGreatSeparator() {
            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialise(this.Interface);

            this.ConfigureInstructions();

            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);

            if (this.SigScanner.TryScanText(Signatures.ShowFlyText, out var showFlyPtr)) {
                this.ShowFlyTextHook = new Hook<ShowFlyTextDelegate>(showFlyPtr + 9, this.ShowFlyTextDetour);
                this.ShowFlyTextHook.Enable();
            }

            if (this.SigScanner.TryScanText(Signatures.SprintfNumber, out var sprintfPtr)) {
                this.SprintfNumberHook = new Hook<SprintfNumberDelegate>(sprintfPtr, this.SprintfNumberDetour);
                this.SprintfNumberHook.Enable();
            }

            if (this.SigScanner.TryGetStaticAddressFromSig(Signatures.Separator, out var separatorPtr)) {
                this.SeparatorPtr = separatorPtr;
                this.OriginalSeparator = Marshal.ReadByte(this.SeparatorPtr);
            }

            this.SetSeparator(this.Config.CustomSeparator);
        }

        public void Dispose() {
            this.SprintfNumberHook?.Dispose();
            this.ShowFlyTextHook?.Dispose();
            this.RestoreAllBytes();
            this.Commands.Dispose();
            this.Ui.Dispose();
            if (this.SeparatorPtr != IntPtr.Zero && this.OriginalSeparator != 0) {
                Marshal.WriteByte(this.SeparatorPtr, this.OriginalSeparator);
            }
        }

        internal void SetSeparator(char? sep) {
            if (this.SeparatorPtr == IntPtr.Zero) {
                return;
            }

            var separator = (byte?) sep ?? this.OriginalSeparator;
            if (separator == 0) {
                separator = (byte) ',';
            }

            Marshal.WriteByte(this.SeparatorPtr, separator);
        }

        internal void ConfigureInstructions() {
            this.ConfigureInstruction(Signatures.FlyTextStringify, this.Config.FlyText);
            this.ConfigureInstruction(Signatures.HotbarManaStringify, this.Config.AbilityCost);
            this.ConfigureInstruction(Signatures.PartyListStringify, this.Config.PartyList);
        }

        private void ConfigureInstruction(string sig, bool enabled) {
            if (!this.SigScanner.TryScanText(sig, out var ptr)) {
                return;
            }

            if (enabled) {
                this.ReplaceBytes(ptr);
            } else {
                this.RestoreBytes(ptr);
            }
        }

        private void ReplaceBytes(IntPtr ptr) {
            if (this.OldBytes.ContainsKey(ptr)) {
                return;
            }

            SafeMemory.ReadBytes(ptr, ThirdArgOne.Length, out var oldBytes);
            SafeMemory.WriteBytes(ptr, ThirdArgOne);
            this.OldBytes[ptr] = oldBytes;
        }

        private void RestoreBytes(IntPtr ptr) {
            if (!this.OldBytes.TryGetValue(ptr, out var oldBytes)) {
                return;
            }

            SafeMemory.WriteBytes(ptr, oldBytes);
            this.OldBytes.Remove(ptr);
        }

        private void RestoreAllBytes() {
            foreach (var ptr in this.OldBytes.Keys.ToList()) {
                this.RestoreBytes(ptr);
            }
        }

        private unsafe IntPtr SprintfNumberDetour(uint number) {
            var ret = (byte*) this.SprintfNumberHook!.Original(number);
            if (!this.Config.AbilityTooltip) {
                goto Return;
            }

            var nfi = (NumberFormatInfo) NumberFormatInfo.CurrentInfo.Clone();
            if (this.Config.CustomSeparator != null) {
                nfi.NumberGroupSeparator = this.Config.CustomSeparator.ToString();
            }

            var str = number.ToString("N0", nfi);
            var strBytes = Encoding.UTF8.GetBytes(str);
            fixed (byte* bytesPtr = strBytes) {
                Buffer.MemoryCopy(bytesPtr, ret, 0x40, strBytes.Length);
            }

            *(ret + strBytes.Length) = 0;

            Return:
            return (IntPtr) ret;
        }

        private unsafe void ShowFlyTextDetour(IntPtr addon, uint actorIndex, uint messageMax, IntPtr numbers, int offsetNum, int offsetNumMax, IntPtr strings, int offsetStr, int offsetStrMax, int a10) {
            this.ShowFlyTextHook!.Original(addon, actorIndex, messageMax, numbers, offsetNum, offsetNumMax, strings, offsetStr, offsetStrMax, a10);

            if (!this.Config.FlyText) {
                return;
            }

            static void Action(IntPtr ptr) {
                // only check text nodes
                var node = (AtkResNode*) ptr;
                if (node->Type != NodeType.Text) {
                    return;
                }

                var text = (AtkTextNode*) node;
                var font = (text->AlignmentFontType & 0xF0) >> 4;
                // only touch text nodes with a font above four and less than eight
                if (font is not (> 4 and < 8)) {
                    return;
                }

                // only touch text nodes with a string starting with a digit
                var stringPtr = text->NodeText.StringPtr;
                if (stringPtr == null || !char.IsDigit((char) *stringPtr)) {
                    return;
                }

                // set the font type of the node to 4 for non-number support
                text->AlignmentFontType = (byte) ((text->AlignmentFontType & 0xF) | (4 << 4));
            }

            var unit = (AtkUnitBase*) addon;
            if (unit->RootNode != null) {
                this.TraverseNodes(unit->RootNode, Action);
            }

            for (var i = 0; i < unit->UldManager.NodeListCount; i++) {
                var node = unit->UldManager.NodeList[i];
                this.TraverseNodes(node, Action);
            }
        }

        private unsafe void TraverseNodes(AtkResNode* node, Action<IntPtr> action, bool siblings = true) {
            if (node == null) {
                return;
            }

            action((IntPtr) node);

            if ((int) node->Type < 1000) {
                this.TraverseNodes(node->ChildNode, action);
            } else {
                var comp = (AtkComponentNode*) node;

                for (var i = 0; i < comp->Component->UldManager.NodeListCount; i++) {
                    this.TraverseNodes(comp->Component->UldManager.NodeList[i], action);
                }
            }

            if (!siblings) {
                return;
            }

            var prev = node;
            while ((prev = prev->PrevSiblingNode) != null) {
                this.TraverseNodes(prev, action, false);
            }

            var next = node;
            while ((next = next->NextSiblingNode) != null) {
                this.TraverseNodes(next, action, false);
            }
        }
    }
}
