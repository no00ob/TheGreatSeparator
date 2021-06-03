using Dalamud.Plugin;

namespace TheGreatSeparator {
    public class Plugin : IDalamudPlugin {
        public string Name => "The Great Separator";

        private TheGreatSeparator? Separator { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Separator = new TheGreatSeparator(pluginInterface);
        }

        public void Dispose() {
            this.Separator?.Dispose();
        }
    }
}
