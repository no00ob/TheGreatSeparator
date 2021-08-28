using System;
using Dalamud.Game.Command;

namespace TheGreatSeparator {
    public class Commands : IDisposable {
        private TheGreatSeparator Plugin { get; }

        internal Commands(TheGreatSeparator plugin) {
            this.Plugin = plugin;

            this.Plugin.CommandManager.AddHandler("/tgs", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open The Great Separator",
            });
        }

        public void Dispose() {
            this.Plugin.CommandManager.RemoveHandler("/tgs");
        }

        private void OnCommand(string command, string args) {
            this.Plugin.Ui.Toggle();
        }
    }
}
