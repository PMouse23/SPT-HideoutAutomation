#nullable enable

using HideoutAutomation.Helpers;

namespace HideoutAutomation.Extensions
{
    internal static class HideoutClassExtensions
    {
        public static void StartProducing(this HideoutClass hideoutClass, ProductionBuildAbstractClass scheme)
        {
            string schemeId = scheme._id;
            if (schemeId == null)
                return;
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification($"TryStartProducing {schemeId}.");
            foreach (var kvp in hideoutClass.ProductionController.Dictionary_0)
                foreach (var producer in kvp.Value)
                    if (producer.Schemes.ContainsKey(schemeId))
                    {
                        var producingItem = scheme.GetProducingItem(producer.ProductionSpeedCoefficient, producer.ReductionCoefficient);
                        if (producingItem != null && producingItem.SchemeId != null)
                        {
                            if (producer.IsWorking)
                            {
                                if (Globals.Debug)
                                    LogHelper.LogInfoWithNotification($"producer IsWorking.");
                                return;
                            }
                            producer.StartProducing(scheme);
                            if (Globals.Debug)
                                LogHelper.LogInfoWithNotification($"StartProducing {schemeId} {scheme.productionTime}.");
                        }
                    }
        }
    }
}