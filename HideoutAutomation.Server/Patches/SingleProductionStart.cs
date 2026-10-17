using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace HideoutAutomation.Server.Patches;

[Injectable]
public class SingleProductionStart : AbstractPatch
{
    private static HideoutAutomationService _service = default!;
    private static EventOutputHolder _output = default!;
    private static HttpResponseUtil _response = default!;
    public SingleProductionStart(HideoutAutomationService service, EventOutputHolder output, HttpResponseUtil response)
    {
        _service = service;
        _output = output;
        _response = response;
    }

    [PatchPrefix]
    public static bool Prefix(PmcData pmcData, HideoutSingleProductionStartRequestData request, MongoId sessionID,
        ref ItemEventRouterResponse __result)
    {
        if (!_service.ShouldStack(sessionID, pmcData, request)) return true;
        __result = _output.GetOutput(sessionID);
        if (!_service.Stack(sessionID, request).GetAwaiter().GetResult())
            _response.AppendErrorToOutput(__result, "Unable to queue this craft: its ingredients could not be reserved.");
        return false;
    }

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(HideoutController), nameof(HideoutController.SingleProductionStart));
}
