using HideoutAutomation.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Utils;

namespace HideoutAutomation.Server
{
    [Injectable]
    public class HideoutAutomationRouter(JsonUtil jsonUtil, HideoutAutomationService automationService) : StaticRouter(jsonUtil, [
        new RouteAction<AreaCountRequestData>("/hideoutautomation/AreaCount",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.AreaCount(sessionId, requestData))!),
        new RouteAction<ProductionCountRequestData>("/hideoutautomation/StackCount",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.StackCount(sessionId, requestData))!),
        new RouteAction<HideoutSingleProductionStartRequestData>("/hideoutautomation/Stack",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.Stack(sessionId, requestData))!),
        new RouteAction<NextProductionRequestData>("/hideoutautomation/StartFromStack",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.StartFromStack(sessionId, requestData))!)
        ])
    {
    }
}