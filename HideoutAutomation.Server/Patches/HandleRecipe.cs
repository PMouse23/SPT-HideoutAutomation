using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using System.Reflection;

namespace HideoutAutomation.Server.Patches;

[Injectable]
public class HandleRecipe : AbstractPatch
{
    private static HideoutAutomationService _service = default!;
    private static readonly AsyncLocal<CollectionContext?> Current = new();
    public HandleRecipe(HideoutAutomationService service) => _service = service;
    public sealed record CollectionContext(PmcData Profile, HideoutSingleProductionStartRequestData[] Pending)
    {
        public List<HideoutSingleProductionStartRequestData> Delivered { get; } = [];
    }
    public static PmcData? LastCalledPmcData => Current.Value?.Profile;
    public static HideoutSingleProductionStartRequestData[] PendingProductions => Current.Value?.Pending ?? [];
    public static void MarkDelivered(HideoutSingleProductionStartRequestData request) => Current.Value?.Delivered.Add(request);

    [PatchPrefix]
    public static void Prefix(PmcData pmcData, MongoId sessionID, out CollectionContext? __state)
    {
        __state = Current.Value;
        Current.Value = _service.IsQueueEnabled(sessionID)
            ? new(pmcData, _service.GetCompletedRequests(pmcData)) : null;
    }

    [PatchPostfix]
    public static void Postfix(PmcData pmcData, ItemEventRouterResponse output)
    {
        if (Current.Value is { } context && output.Warnings?.Count is not > 0)
            _service.AcknowledgeCompleted(pmcData, context.Delivered.ToArray());
    }

    [PatchFinalizer]
    public static Exception? Finalizer(Exception? __exception, CollectionContext? __state)
    {
        Current.Value = __state;
        return __exception;
    }

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(HideoutController), "HandleRecipe");
}
