using HideoutAutomation.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace HideoutAutomation.Server
{
    [Injectable]
    public class HideoutAutomationRouter(JsonUtil jsonUtil, HideoutAutomationService automationService) : StaticRouter(jsonUtil, [
        new RouteAction<FindItemsRequestData>("/hideoutautomation/CanFindAllItems",
            async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.CanFindAllItems(sessionId, requestData))!),
        ])
    {
    }
}