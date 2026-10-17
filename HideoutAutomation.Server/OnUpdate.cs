using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(TypePriority = OnLoadOrder.SaveCallbacks)]
    public class OnUpdate(HideoutConfig hideoutConfig, SaveServer saveServer, HideoutAutomationService hideoutAutomationService) : IOnUpdate
    {
        protected readonly HideoutConfig HideoutConfig = hideoutConfig;

        Task<bool> IOnUpdate.OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)
        {
            if (secondsSinceLastRun < HideoutConfig.RunIntervalSeconds)
                return Task.FromResult(false);

            foreach (var (sessionId, profile) in saveServer.GetProfiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!hideoutAutomationService.IsQueueEnabled(sessionId)) continue;
                //HACK first few versions for compatibility.
                hideoutAutomationService.MoveOldtoNewQueue(sessionId, profile.CharacterData?.PmcData);
                hideoutAutomationService.UpdateProductionQueue(sessionId, profile.CharacterData?.PmcData);
            }
            return Task.FromResult(true);
        }
    }
}
