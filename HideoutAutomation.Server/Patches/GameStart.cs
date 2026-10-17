using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using System.Linq;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    [SPTarkov.DI.Annotations.Injectable]
    public class GameStart : AbstractPatch
    {
        private static HideoutAutomationService _hideoutAutomationService = default!;
        private static ProfileHelper _profileHelper = default!;
        private static SPTarkov.Server.Core.Models.Spt.Tables.HideoutTable _hideoutTable = default!;
        public GameStart(HideoutAutomationService hideoutAutomationService, ProfileHelper profileHelper, SPTarkov.Server.Core.Models.Spt.Tables.HideoutTable hideoutTable)
        {
            _hideoutAutomationService = hideoutAutomationService;
            _profileHelper = profileHelper;
            _hideoutTable = hideoutTable;
        }

        [PatchPostfix]
        public static void PatchPostfix(MongoId sessionId)
        {
            if (_hideoutAutomationService is HideoutAutomationService automationService)
            {
                automationService.SetQueueEnabled(sessionId, false).GetAwaiter().GetResult();
            }

            //HACK Fix player profile
            if (_profileHelper is ProfileHelper profileHelper
                && _hideoutTable is SPTarkov.Server.Core.Models.Spt.Tables.HideoutTable databaseService)
            {
                var hideout = databaseService;
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