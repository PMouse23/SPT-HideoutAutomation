using BepInEx.Bootstrap;

namespace HideoutAutomation.Helpers
{
    internal class ModChecker
    {
        internal static bool IsModLoaded(string modKey)
        {
            return Chainloader.PluginInfos.ContainsKey(modKey);
        }
    }
}