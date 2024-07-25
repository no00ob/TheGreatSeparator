using Dalamud.Configuration;
using Dalamud.Plugin;

namespace TheGreatSeparator {
    public class Configuration : IPluginConfiguration {
        private IDalamudPluginInterface Interface { get; set; } = null!;

        public int Version { get; set; } = 1;

        public bool FlyText = true;
        public bool AbilityCost;
        public bool AbilityTooltip;
        public bool PartyList = true;
        public char? CustomSeparator;

        internal void Initialise(IDalamudPluginInterface @interface) {
            this.Interface = @interface;
        }

        internal void Save() {
            this.Interface.SavePluginConfig(this);
        }
    }
}
