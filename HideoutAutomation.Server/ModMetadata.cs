using SPTarkov.Server.Core.Models.Spt.Mod;
using System.Collections.Generic;

namespace HideoutAutomation.Server
{
    public record ModMetadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = "com.KnotScripts.HideoutAutomation";
        public override string Name { get; init; } = "HideoutAutomation";

        public override string Author { get; init; } = "KnotScripts";

        public override List<string> Contributors { get; init; }

        public override SemanticVersioning.Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version?.ToString(3));

        public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

        public override List<string> Incompatibilities { get; init; }

        public override Dictionary<string, SemanticVersioning.Range> ModDependencies { get; init; }

        public override string Url { get; init; } = "https://github.com/PMouse23/SPT-HideoutAutomation";

        public override bool? IsBundleMod { get; init; } = false;

        public override string License { get; init; } = "MIT";
    }
}