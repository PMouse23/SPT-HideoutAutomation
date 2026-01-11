using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Services;
using System.Linq;
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

            //HACK Fix player profile
            if (ServiceLocator.ServiceProvider.GetService<ProfileHelper>() is ProfileHelper profileHelper
                && ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService)
            {
                var hideout = databaseService.GetHideout();
                if (hideout == null)
                    return;
                var pmcData = profileHelper.GetPmcProfile(sessionId);
                if (pmcData == null)
                    return;
                var areas = pmcData.Hideout?.Areas;
                if (areas == null)
                    return;
                foreach (var profileHideoutArea in areas)
                {
                    var hideoutData = hideout.Areas.FirstOrDefault(area => area.Type == profileHideoutArea.Type);
                    if (hideoutData == null)
                        continue;
                    int? level = profileHideoutArea.Level;
                    if (level == null)
                        continue;
                    string? levelString = level.ToString();
                    if (levelString == null)
                        continue;
                    int maxLevel = hideoutData.Stages?.Max(stage => int.TryParse(stage.Key, out int level) ? level : 0) ?? 0;
                    if (level > maxLevel)
                        profileHideoutArea.Level = maxLevel;
                }
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameController), nameof(GameController.GameStart));
        }
    }
}