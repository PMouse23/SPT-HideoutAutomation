using HideoutAutomation.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace HideoutAutomation.Server
{
    [Injectable]
    public class HideoutAutomationRouter(JsonUtil jsonUtil, HideoutAutomationService automationService) : StaticRouter(jsonUtil, [
        new RouteAction<ProductionCountRequestData>("/hideoutautomation/StackCount",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.StackCount(sessionId, requestData))!),
        new RouteAction<ProductionPreservationRequestData>("/hideoutautomation/preservation",
                async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.MakePreservation(sessionId, requestData))!)
        ])
    {
    }
}