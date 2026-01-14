using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Linq;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class UpgradeComplete : AbstractPatch
    {
        [PatchPrefix]
        public static bool PatchPrefix(PmcData pmcData, HideoutUpgradeCompleteRequestData request, MongoId sessionID, ItemEventRouterResponse output)
        {
            if (ServiceLocator.ServiceProvider.GetService<DatabaseService>() is DatabaseService databaseService
                && ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>() is HttpResponseUtil httpResponseUtil
                && ServiceLocator.ServiceProvider.GetService<ISptLogger<HideoutController>>() is ISptLogger<HideoutController> logger
                && ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>() is ServerLocalisationService serverLocalisationService)
            {
                var hideout = databaseService.GetHideout();
                var profileHideoutArea = pmcData.Hideout?.Areas?.FirstOrDefault(area => area.Type == request.AreaType);
                if (profileHideoutArea is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area", request.AreaType));
                    httpResponseUtil.AppendErrorToOutput(output);
                    return false;
                }
                var hideoutData = hideout.Areas.FirstOrDefault(area => area.Type == profileHideoutArea.Type);
                if (hideoutData is null)
                {
                    logger.Error(serverLocalisationService.GetText("hideout-unable_to_find_area_in_database", request.AreaType));
                    httpResponseUtil.AppendErrorToOutput(output);
                    return false;
                }
                int? level = profileHideoutArea.Level;
                if (level is null)
                {
                    logger.Error($"Stage level is null.");
                    httpResponseUtil.AppendErrorToOutput(output);
                    return false;
                }
                level++;
                string? levelString = level.ToString();
                Stage? hideoutStage = null;
                if (levelString is null
                    || hideoutData.Stages?.TryGetValue(levelString, out hideoutStage) == false)
                {
                    logger.Error($"Stage level: {level} not found for area: {request.AreaType}");
                    return false;
                }
                var constructionTime = hideoutStage?.ConstructionTime ?? 0;
                if (profileHideoutArea.Constructing == false && constructionTime > 0)
                {
                    logger.Error($"Area: {request.AreaType} not constructing.");
                    httpResponseUtil.AppendErrorToOutput(output);
                    return false;
                }
            }
            return true;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.UpgradeComplete));
        }
    }
}