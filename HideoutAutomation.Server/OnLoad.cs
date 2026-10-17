using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

namespace HideoutAutomation.Server;

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class OnLoad(IEnumerable<IRuntimePatch> patches, ISptLogger<OnLoad> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        foreach (var patch in patches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            patch.Enable();
        }
        logger.Info("HideoutAutomation 1.3.2: construction/HIP enabled; production queue requires client opt-in.");
        return Task.CompletedTask;
    }
}
