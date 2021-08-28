using System;
using ImGuiNET;

namespace TheGreatSeparator {
    internal class PluginUi : IDisposable {
        private TheGreatSeparator Plugin { get; }

        private bool _showWindow;

        internal PluginUi(TheGreatSeparator plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.UiBuilder.Draw += this.Draw;
            this.Plugin.Interface.UiBuilder.OpenConfigUi += this.Toggle;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OpenConfigUi -= this.Toggle;
            this.Plugin.Interface.UiBuilder.Draw -= this.Draw;
        }

        internal void Toggle() {
            this._showWindow = !this._showWindow;
        }

        private void Draw() {
            if (!this._showWindow) {
                return;
            }

            if (!ImGui.Begin("The Great Separator", ref this._showWindow)) {
                ImGui.End();
                return;
            }

            var save = false;
            save |= ImGui.Checkbox("Add separators to damage/healing numbers", ref this.Plugin.Config.FlyText);
            save |= ImGui.Checkbox("Add separators to party list HP", ref this.Plugin.Config.PartyList);
            save |= ImGui.Checkbox("Add separators to ability costs on hotbars", ref this.Plugin.Config.AbilityCost);
            save |= ImGui.Checkbox("Add separators to ability costs in tooltips", ref this.Plugin.Config.AbilityTooltip);

            var custom = this.Plugin.Config.CustomSeparator?.ToString() ?? string.Empty;
            if (ImGui.InputText("Custom separator", ref custom, 1)) {
                save = true;
                this.Plugin.Config.CustomSeparator = string.IsNullOrEmpty(custom) ? null : custom[0];
                this.Plugin.SetSeparator(this.Plugin.Config.CustomSeparator);
            }

            if (save) {
                this.Plugin.Config.Save();
                this.Plugin.ConfigureInstructions();
            }

            ImGui.End();
        }
    }
}
