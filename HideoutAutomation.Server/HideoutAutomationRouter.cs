using HideoutAutomation.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Utils;

namespace HideoutAutomation.Server
{
    [Injectable]
    public class HideoutAutomationRouter(JsonUtil jsonUtil, HideoutAutomationService automationService) : StaticRouter(jsonUtil, [
        new RouteAction("/hideoutautomation/State",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.GetState(sessionId))!),
        new RouteAction<HideoutSingleProductionStartRequestData>("/hideoutautomation/Stack",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.Stack(sessionId, requestData))!),
        new RouteAction<NextProductionRequestData>("/hideoutautomation/StartFromStack",
                 async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.StartFromStack(sessionId, requestData))!),
        new RouteAction<FindItemsRequestData>("/hideoutautomation/CanFindAllItems",
            async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.CanFindAllItems(sessionId, requestData))!),
        new RouteAction<FindProductionRequestData>("/hideoutautomation/CanFindProduction",
            async (url, requestData, sessionId, output) => jsonUtil.Serialize(await automationService.CanFindProduction(sessionId, requestData))!),
        ])
    {
    }
}