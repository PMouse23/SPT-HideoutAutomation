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

        public HideoutAutomationData Get(MongoId profileId)
        {
            if (hideoutAutomationData.TryGetValue(profileId, out HideoutAutomationData? data))
                return data;

            data = jsonStore.Load(profileId, filename);
            if (data == null)
                data = new HideoutAutomationData();
            hideoutAutomationData[profileId] = data;
            return data;
        }

        public void Set(MongoId profileId)
        {
            if (hideoutAutomationData.TryGetValue(profileId, out HideoutAutomationData? data))
                jsonStore.Save(data, profileId, filename);
        }
    }
}