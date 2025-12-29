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
    public class SingleProductionStart : AbstractPatch
    {
        [PatchPrefix]
        public static bool PatchPrefix(PmcData pmcData, HideoutSingleProductionStartRequestData request, MongoId sessionID)
        {
            bool result = true;
            if (ServiceLocator.ServiceProvider.GetService<HideoutAutomationService>() is HideoutAutomationService automationService)
            {
                bool shouldStack = automationService.ShouldStack(sessionID, pmcData, request);
                if (shouldStack)
                {
                    automationService.Stack(sessionID, request);
                    result = false;
                }
            }
            return result;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.SingleProductionStart));
        }
    }
}