using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class GameStart : AbstractPatch
    {
        [PatchPostfix]
        public static void PatchPostfix(MongoId sessionId)
        {
            if (ServiceLocator.ServiceProvider.GetService<HideoutAutomationService>() is HideoutAutomationService automationService)
            {
                automationService.StartFromStack(sessionId);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameController), nameof(GameController.GameStart));
        }
    }
}