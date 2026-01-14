using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Eft.Common;
using System;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class HandleRecipe : AbstractPatch
    {
        [ThreadStatic]
        public static PmcData? LastCalledPmcData;

        [PatchPrefix]
        public static void PatchPrefix(PmcData pmcData)
        {
            LastCalledPmcData = pmcData;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), "HandleRecipe");
        }
    }
}