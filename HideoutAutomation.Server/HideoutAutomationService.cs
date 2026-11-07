using HideoutAutomation.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using System.Linq;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(InjectionType = InjectionType.Singleton)]
    public class HideoutAutomationService(
        ProfileHelper profileHelper
        )
    {
        public ValueTask<bool> CanFindAllItems(MongoId sessionId, FindItemsRequestData requestData)
        {
            if (requestData.ItemIds == null)
                return ValueTask.FromResult(false);
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(false);
            foreach (MongoId itemId in requestData.ItemIds)
            {
                bool? found = pmcData.Inventory?.Items?.Any(i => i.Id == itemId);
                if (found != true)
                    return ValueTask.FromResult(false);
            }
            return ValueTask.FromResult(true);
        }
    }
}