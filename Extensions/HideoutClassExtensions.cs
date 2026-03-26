#nullable enable

using Comfort.Common;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production;

namespace HideoutAutomation.Extensions
{
    internal static class HideoutClassExtensions
    {
        public static bool StartProducing(this HideoutClass hideoutClass, ProductionBuildAbstractClass scheme)
        {
            string schemeId = scheme._id;
            if (schemeId == null)
                return false;
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification($"TryStartProducing {schemeId}.");
            foreach (var kvp in hideoutClass.ProductionController.Dictionary_0)
                foreach (var producer in kvp.Value)
                    if (producer.Schemes.ContainsKey(schemeId))
                    {
                        var producingItem = scheme.GetProducingItem(producer.ProductionSpeedCoefficient, producer.ReductionCoefficient);
                        if (producingItem != null && producingItem.SchemeId != null)
                        {
                            scheme.productionTime = Singleton<ProductionService>.Instance.CalculateProductionTime(producingItem.SchemeId, () =>
                            {
                                return (float)producer.CalculateProductionTime(scheme);
                            });
                            producer.StartProducing(scheme);
                            if (Globals.Debug)
                                LogHelper.LogInfoWithNotification($"StartProducing {schemeId} {scheme.productionTime}.");
                            return true;
                        }
                    }
            if (Globals.Debug)
                LogHelper.LogErrorWithNotification($"StartProducing {schemeId} failed.");
            return false;
        }
    }
}