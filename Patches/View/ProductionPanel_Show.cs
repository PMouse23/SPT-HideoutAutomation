using Comfort.Common;
using EFT.Hideout;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace HideoutAutomation.Patches.View
{
    internal class ProductionPanel_Show : ModulePatch
    {
        [PatchPostfix]
        public static async void PatchPostfix(ProductionPanel __instance)
        {
            try
            {
                await Singleton<ProductionService>.Instance.GetState();
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionToConsole(ex);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(ProductionPanel), this.isTargetMethod);
        }

        private bool isTargetMethod(MethodInfo method)
        {
            return method.Name == nameof(ProductionPanel.Show);
        }
    }
}