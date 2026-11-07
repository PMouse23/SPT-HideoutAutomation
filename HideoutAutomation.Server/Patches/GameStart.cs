using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using System.Linq;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class GameStart : AbstractPatch
    {
        [PatchPostfix]
        public static void PatchPostfix(MongoId sessionId)
        {
            //HACK Fix player profile
            if (ModServices.Get<ProfileHelper>() is ProfileHelper profileHelper
                && ModServices.Get<HideoutTable>() is HideoutTable hideoutTable)
            {
                var hideout = hideoutTable;
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