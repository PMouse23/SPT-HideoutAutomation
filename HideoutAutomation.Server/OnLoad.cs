using HideoutAutomation.Server.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(TypePriority = OnLoadOrder.Preload)]
    public class OnLoad(IServiceProvider serviceProvider) : IOnLoad
    {
        Task IOnLoad.OnLoadAsync(CancellationToken cancellationToken)
        {
            ModServices.Provider = serviceProvider;

            new GameStart().Enable();
            new MoveItem().Enable();
            new UpgradeComplete().Enable();

            return Task.CompletedTask;
        }
    }
}
