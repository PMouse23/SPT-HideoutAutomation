using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(TypePriority = OnLoadOrder.SaveCallbacks)]
    public class OnUpdate(ConfigServer configServer, SaveServer saveServer, HideoutAutomationService hideoutAutomationService) : IOnUpdate
    {
        protected readonly HideoutConfig HideoutConfig = configServer.GetConfig<HideoutConfig>();

        async Task<bool> IOnUpdate.OnUpdate(long secondsSinceLastRun)
        {
            if (secondsSinceLastRun < HideoutConfig.RunIntervalSeconds)
                return false;

            foreach (var (sessionId, profile) in saveServer.GetProfiles())
                hideoutAutomationService.UpdateProductionQueue(sessionId, profile.CharacterData?.PmcData);
            return true;
        }
    }
}