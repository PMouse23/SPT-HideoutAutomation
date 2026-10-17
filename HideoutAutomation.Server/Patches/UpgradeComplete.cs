using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace HideoutAutomation.Server.Patches;

[Injectable]
public class UpgradeComplete : AbstractPatch
{
    private static HideoutTable _hideout = default!;
    private static HttpResponseUtil _http = default!;
    private static ISptLogger<UpgradeComplete> _logger = default!;

    public UpgradeComplete(HideoutTable hideout, HttpResponseUtil http, ISptLogger<UpgradeComplete> logger)
    {
        _hideout = hideout;
        _http = http;
        _logger = logger;
    }

    [PatchPrefix]
    public static bool PatchPrefix(PmcData pmcData, HideoutUpgradeCompleteRequestData request, MongoId sessionID, ItemEventRouterResponse output)
    {
        var area = pmcData.Hideout?.Areas?.FirstOrDefault(candidate => candidate.Type == request.AreaType);
        var template = _hideout.Areas.FirstOrDefault(candidate => candidate.Type == request.AreaType);
        if (area?.Level is not int level || template?.Stages is null ||
            !template.Stages.TryGetValue((level + 1).ToString(), out var stage) ||
            (area.Constructing != true && stage.ConstructionTime > 0))
        {
            _logger.Warning($"HideoutAutomation: ignored stale upgrade completion for area {request.AreaType}.");
            _http.AppendErrorToOutput(output, $"HideoutAutomation: area {request.AreaType} is not awaiting installation.");
            return false;
        }
        return true;
    }

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(HideoutController), nameof(HideoutController.UpgradeComplete));
}
