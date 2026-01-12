using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class UnstackRewardIntoValidSize : AbstractPatch
    {
        [PatchPostfix]
        public static void PatchPostfix(HideoutProduction recipe, List<List<Item>> itemAndChildrenToSendToPlayer, bool rewardIsPreset)
        {
            /*
             * TODO 1. Patch HideoutController.UnstackRewardIntoValidSize to give all stacked production over time. Even when server was clossed.
             * TODO 2. First you need to move stacked recipes to completed stack on startup and overtime (during raid).
             * TODO 3. When UnstackRewardIntoValidSize is called you [PatchPostfix] to check how many times you have to extra call the UnstackRewardIntoValidSize function.
             * TODO 4. When you get back out or raid the full stack is returned to you.
             */

            int multiply = 0; //TODO times to multiply reward.
            List<Item>? first = itemAndChildrenToSendToPlayer.FirstOrDefault();
            if (first == null)
                return;
            if (ServiceLocator.ServiceProvider.GetService<ICloner>() is ICloner cloner)
                for (int i = 0; i < multiply; i++)
                {
                    List<Item>? firstItemWithChildrenClone = cloner.Clone(first)?.ReplaceIDs().ToList();
                    if (firstItemWithChildrenClone != null)
                        itemAndChildrenToSendToPlayer.AddRange(firstItemWithChildrenClone);
                }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), "UnstackRewardIntoValidSize");
        }
    }
}