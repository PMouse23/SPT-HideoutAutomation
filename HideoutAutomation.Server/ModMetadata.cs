using SPTarkov.Server.Core.Models.Spt.Mod;

namespace HideoutAutomation.Server;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.KnotScripts.HideoutAutomation";
    public string Name { get; init; } = "HideoutAutomation";
    public string Author { get; init; } = "KnotScripts";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version!.ToString(3));
    public SemanticVersioning.Range SptVersion { get; init; } = new(">=4.1.4 <4.2.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/PMouse23/SPT-HideoutAutomation";
    public bool HasPrepatcher { get; init; }
    public string License { get; init; } = "MIT";
}
