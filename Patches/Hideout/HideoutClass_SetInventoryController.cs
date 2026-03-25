using Comfort.Common;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace HideoutAutomation.Patches.Hideout
{
    internal class HideoutClass_SetInventoryController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(HideoutClass), this.IsTargetMethod);
        }

        [PatchPostfix]
        private static async void PatchPostfix(HideoutClass __instance)
        {
            if (Singleton<ProductionService>.Instance != null)
                return;
            Singleton<ProductionService>.Create(new ProductionService());
            if (Globals.Debug)
                LogHelper.LogInfo($"Created ProductionService");
            await tryGetState();
        }

        private static async Task tryGetState()
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

        private bool IsTargetMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return method.Name == nameof(HideoutClass.SetInventoryController)
                && parameters.Length != 1;
        }
    }
}