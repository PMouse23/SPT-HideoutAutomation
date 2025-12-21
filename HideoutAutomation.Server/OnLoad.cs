using HideoutAutomation.Server.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
    public class OnLoad : IOnLoad
    {
        Task IOnLoad.OnLoad()
        {
            new GameStart().Enable();
            new HideoutTakeProduction().Enable();
            new SingleProductionStart().Enable();

            return Task.CompletedTask;
        }
    }
}