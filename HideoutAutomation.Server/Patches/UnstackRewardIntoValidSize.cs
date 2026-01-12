using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
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
            PmcData? pmcData = HandleRecipe.LastCalldedPmcData;
            if (pmcData == null)
                return;
            if (ServiceLocator.ServiceProvider.GetService<HideoutAutomationService>() is not HideoutAutomationService hideoutAutomationService)
                return;
            if (ServiceLocator.ServiceProvider.GetService<ICloner>() is not ICloner cloner)
                return;
            if (recipe.Id is MongoId recipeId)
            {
                int multiply = hideoutAutomationService.CompletedCount(pmcData, recipeId);
                List<List<Item>> extraItemAndChildrenToSendToPlayer = [];
                foreach (List<Item> itemAndChildren in itemAndChildrenToSendToPlayer)
                {
                    for (int i = 0; i < multiply; i++)
                    {
                        List<Item>? firstItemWithChildrenClone = cloner.Clone(itemAndChildren)?.ReplaceIDs().ToList();
                        if (firstItemWithChildrenClone != null)
                            extraItemAndChildrenToSendToPlayer.AddRange(firstItemWithChildrenClone);
                    }
                }
                itemAndChildrenToSendToPlayer.AddRange(extraItemAndChildrenToSendToPlayer);
                hideoutAutomationService.RemoveCompletedProductions(pmcData, recipeId);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), "UnstackRewardIntoValidSize");
        }
    }
}