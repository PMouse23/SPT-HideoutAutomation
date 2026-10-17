using Comfort.Common;
using EFT.Hideout;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace HideoutAutomation.Patches.Hideout
{
    internal class HideoutClass_SetInventoryController : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.FirstMethod(typeof(HideoutRepresentation),
            method => method.Name == nameof(HideoutRepresentation.SetInventoryController) && method.GetParameters().Length != 1);

        [PatchPostfix]
        private static async void PatchPostfix(HideoutRepresentation __instance)
        {
            try
            {
                var previous = Singleton<ProductionService>.Instance;
                if (!Globals.ProductionStacking)
                {
                    previous?.Dispose();
                    Singleton<ProductionService>.Instance = null!;
                    await RequestHandler.PutJsonAsync("/hideoutautomation/QueueEnabled", "{\"enabled\":false}");
                    return;
                }
                if (previous != null && ReferenceEquals(previous.Owner, __instance)) return;
                previous?.Dispose();
                var service = new ProductionService(__instance);
                Singleton<ProductionService>.Create(service);
                await service.SetEnabled(true);
            }
            catch (Exception exception)
            {
                LogHelper.LogExceptionToConsole(exception);
            }
        }
    }
}
