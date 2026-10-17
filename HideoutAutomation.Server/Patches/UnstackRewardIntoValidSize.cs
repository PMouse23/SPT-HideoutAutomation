using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

namespace HideoutAutomation.Server.Patches;

[Injectable]
public class UnstackRewardIntoValidSize : AbstractPatch
{
    private static HideoutTable _hideout = default!;
    private static PresetHelper _presets = default!;
    private static ICloner _cloner = default!;
    private static ItemHelper _items = default!;

    public UnstackRewardIntoValidSize(HideoutTable hideout, PresetHelper presets, ICloner cloner, ItemHelper items)
    {
        _hideout = hideout;
        _presets = presets;
        _cloner = cloner;
        _items = items;
    }

    [PatchPostfix]
    public static void Postfix(List<List<Item>> itemAndChildrenToSendToPlayer)
    {
        if (HandleRecipe.LastCalledPmcData == null) return;
        foreach (var completed in HandleRecipe.PendingProductions)
        {
            var recipe = _hideout.Production.Recipes?.FirstOrDefault(candidate => candidate.Id == completed.RecipeId);
            if (recipe == null || recipe.Count is not > 0) continue;
            var rewards = CreateRewards(recipe);
            itemAndChildrenToSendToPlayer.AddRange(rewards);
            HandleRecipe.MarkDelivered(completed);
        }
    }

    private static List<List<Item>> CreateRewards(HideoutProduction recipe)
    {
        if (_items.IsItemTplStackable(recipe.EndProduct) == true)
            return _items.SplitStackIntoSeparateItems(new Item {
                Id = new MongoId(), Template = recipe.EndProduct, Upd = new() { StackObjectsCount = recipe.Count }
            });

        var preset = _presets.GetDefaultPreset(recipe.EndProduct);
        List<Item> first;
        if (preset?.Items != null)
        {
            first = (_cloner.Clone(preset.Items) ?? throw new InvalidOperationException("Unable to clone queued preset")).ReplaceIDs().ToList();
            first.RemapRootItemId();
        }
        else first = [new() { Id = new MongoId(), Template = recipe.EndProduct }];

        var rewards = new List<List<Item>> { first };
        for (int index = 1; index < recipe.Count; index++)
        {
            var copy = (_cloner.Clone(first) ?? throw new InvalidOperationException("Unable to clone queued reward")).ReplaceIDs().ToList();
            copy.RemapRootItemId();
            rewards.Add(copy);
        }
        return rewards;
    }

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(HideoutController), "UnstackRewardIntoValidSize");
}
