using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class HideoutTakeProduction : AbstractPatch
    {
        [PatchPostfix]
        public static async void PatchPostfix(PmcData pmcData, HideoutTakeProductionRequestData request, MongoId sessionID)
        {
            if (ServiceLocator.ServiceProvider.GetService<HideoutAutomationService>() is HideoutAutomationService automationService)
            {
                automationService.Log($"HideoutAutomation: HideoutTakeProduction");
                automationService.ProduceNext(sessionID, pmcData, request);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.TakeProduction));
        }
    }
}