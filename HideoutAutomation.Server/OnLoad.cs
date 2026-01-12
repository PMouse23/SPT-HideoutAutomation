using HideoutAutomation.Server.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Servers;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
    public class OnLoad(SaveServer saveServer, HideoutAutomationService hideoutAutomationService) : IOnLoad
    {
        Task IOnLoad.OnLoad()
        {
            new GameStart().Enable();
            new MoveItem().Enable();
            new SingleProductionStart().Enable();
            new UnstackRewardIntoValidSize().Enable();
            new UpgradeComplete().Enable();

            return Task.CompletedTask;
        }
    }
}