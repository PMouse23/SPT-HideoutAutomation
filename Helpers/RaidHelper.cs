using Comfort.Common;
using EFT;
using SPT.SinglePlayer.Utils.InRaid;

namespace HideoutAutomation.Helpers
{
    internal static class RaidHelper
    {
        internal static bool HasRaidLoaded()
        {
            return RaidTimeUtil.HasRaidLoaded()
                && Singleton<AbstractGame>.Instance?.GameType != EGameType.Hideout;
        }
    }
}