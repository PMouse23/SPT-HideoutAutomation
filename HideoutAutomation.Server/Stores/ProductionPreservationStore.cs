using HideoutAutomation.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using System.Collections.Concurrent;

namespace HideoutAutomation.Server.Stores
{
    [Injectable(InjectionType = InjectionType.Singleton)]
    public class HideoutAutomationStore(JsonStore<HideoutAutomationData> jsonStore)
    {
        private const string filename = "hideoutautomation.json";

        private readonly ConcurrentDictionary<MongoId, HideoutAutomationData> hideoutAutomationData = [];

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public HideoutAutomationData Get(MongoId profileId)
        {
            if (hideoutAutomationData.TryGetValue(profileId, out HideoutAutomationData? data))
                return data;

            data = jsonStore.Load(profileId, filename);
            if (data == null)
                data = new HideoutAutomationData();
            if (data.CompletedProductions == null || data.AreaProductions == null || data.AreaProductionsAndPayment == null)
                throw new System.IO.InvalidDataException("Invalid saved HideoutAutomation queue. Restore its backup before continuing.");
            hideoutAutomationData[profileId] = data;
            return data;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Set(MongoId profileId)
        {
            if (hideoutAutomationData.TryGetValue(profileId, out HideoutAutomationData? data))
                jsonStore.Save(data, profileId, filename);
        }
    }
}