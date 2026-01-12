using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Eft.Common;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class HandleRecipe : AbstractPatch
    {
        [PatchPrefix]
        public static void PatchPrefix(PmcData pmcData)
        {
            //HACK Little risky but closest to get the PmcData since its not available in UnstackRewardIntoValidSize.
            LastCalldedPmcData = pmcData;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), "HandleRecipe");
        }

        public static PmcData? LastCalldedPmcData { get; set; }
    }
}