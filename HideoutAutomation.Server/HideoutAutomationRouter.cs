using HideoutAutomation.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Utils;

namespace HideoutAutomation.Server
{
    [Injectable(TypePriority = OnLoadOrder.Routers + 1)]
    public class HideoutAutomationRouter(JsonUtil jsonUtil, HideoutAutomationService automationService) : StaticRouter(jsonUtil, [
        new RouteAction<QueueEnabledRequestData>("/hideoutautomation/QueueEnabled",
            async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.SetQueueEnabled(sessionId, requestData.Enabled))!),
        new RouteAction<EmptyQueueRequestData>("/hideoutautomation/State",
                 async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.GetState(sessionId))!),
        new RouteAction<ProductionPreservationRequestData>("/hideoutautomation/Stack",
                 async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.Stack(sessionId, new HideoutSingleProductionStartRequestData { RecipeId = requestData.RecipeId, Items = requestData.Items, Tools = requestData.Tools, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }))!),
        new RouteAction<UnstackProductionRequestData>("/hideoutautomation/Unstack",
                 async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.Unstack(sessionId, requestData))!),
        new RouteAction<NextProductionRequestData>("/hideoutautomation/StartFromStack",
                 async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.StartFromStack(sessionId, requestData))!),
        new RouteAction<FindItemsRequestData>("/hideoutautomation/CanFindAllItems",
            async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.CanFindAllItems(sessionId, requestData))!),
        new RouteAction<FindProductionRequestData>("/hideoutautomation/CanFindProduction",
            async (url, requestData, sessionId, output, cancellationToken) => jsonUtil.Serialize(await automationService.CanFindProduction(sessionId, requestData))!),
        ])
    {
    }
}
