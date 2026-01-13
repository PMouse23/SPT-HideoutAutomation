using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Services;
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
            if (ServiceLocator.ServiceProvider.GetService<HideoutController>() is not HideoutController hideoutController)
                return;
            if (ServiceLocator.ServiceProvider.GetService<DatabaseService>() is not DatabaseService databaseService)
                return;
            if (databaseService.GetHideout() is not SPTarkov.Server.Core.Models.Spt.Hideout.Hideout hideoutDb)
                return;
            IEnumerable<MongoId> completedProductions = hideoutAutomationService.CompletedProductions(pmcData).ToArray();
            foreach (MongoId recipeId in completedProductions)
            {
                HideoutProduction? extra = hideoutDb.Production.Recipes?.FirstOrDefault(r => r.Id == recipeId);
                if (extra is HideoutProduction extraRecipe)
                {
                    bool extraRewardIsPreset = false;
                    List<List<Item>> extraItemAndChildrenToSendToPlayer = HandlePresetReward(extraRecipe);
                    extraRewardIsPreset = extraItemAndChildrenToSendToPlayer.Any();
                    UnstackRewardIntoValidSize2(extraRecipe, extraItemAndChildrenToSendToPlayer, extraRewardIsPreset);
                    itemAndChildrenToSendToPlayer.AddRange(extraItemAndChildrenToSendToPlayer);
                }
                hideoutAutomationService.RemoveCompletedProductions(pmcData, recipeId);
            }
        }

        /// <summary>
        /// Copy from HideoutController.HandlePresetReward.
        /// </summary>
        protected static List<List<Item>> HandlePresetReward(HideoutProduction recipe)
        {
            if (ServiceLocator.ServiceProvider.GetService<PresetHelper>() is not PresetHelper presetHelper)
                return [];
            if (ServiceLocator.ServiceProvider.GetService<ICloner>() is not ICloner cloner)
                return [];
            Preset? defaultPreset = presetHelper.GetDefaultPreset(recipe.EndProduct);
            if (defaultPreset == null)
                return [];
            // Ensure preset has unique ids and is cloned so we don't alter the preset data stored in memory
            List<Item>? presetAndModsClone = cloner.Clone(defaultPreset.Items)?.ReplaceIDs().ToList();

            presetAndModsClone?.RemapRootItemId();

            // Store preset items in array
            return [presetAndModsClone];
        }

        protected static void UnstackRewardIntoValidSize2(HideoutProduction recipe, List<List<Item>> itemAndChildrenToSendToPlayer, bool rewardIsPreset)
        {
            if (ServiceLocator.ServiceProvider.GetService<ItemHelper>() is not ItemHelper itemHelper)
                return;
            if (ServiceLocator.ServiceProvider.GetService<ICloner>() is not ICloner cloner)
                return;
            var rewardIsStackable = itemHelper.IsItemTplStackable(recipe.EndProduct);
            if (rewardIsStackable.GetValueOrDefault(false))
            {
                // Create root item
                var rewardToAdd = new Item
                {
                    Id = new MongoId(),
                    Template = recipe.EndProduct,
                    Upd = new Upd { StackObjectsCount = recipe.Count },
                };

                // Split item into separate items with acceptable stack sizes
                var splitReward = itemHelper.SplitStackIntoSeparateItems(rewardToAdd);
                itemAndChildrenToSendToPlayer.AddRange(splitReward);

                return;
            }

            // Not stackable, may have to send multiple of reward

            // Add the first reward item to array when not a preset (first preset added above earlier)
            if (!rewardIsPreset)
            {
                itemAndChildrenToSendToPlayer.Add([new Item { Id = new MongoId(), Template = recipe.EndProduct }]);
            }

            // Add multiple of item if recipe requests it
            // Start index at one so we ignore first item in array
            var countOfItemsToReward = recipe.Count;
            for (var index = 1; index < countOfItemsToReward; index++)
            {
                var firstItemWithChildrenClone = cloner.Clone(itemAndChildrenToSendToPlayer.FirstOrDefault()).ReplaceIDs().ToList();

                itemAndChildrenToSendToPlayer.AddRange([firstItemWithChildrenClone]);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), "UnstackRewardIntoValidSize");
        }
    }
}