using Comfort.Common;
using EFT;
using HideoutAutomation.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace HideoutAutomation.Production
{
    internal class ProductionService
    {
        private Dictionary<EAreaType, string> currentlyProducing = new Dictionary<EAreaType, string>();

        public ProductionService()
        {
            Singleton<HideoutClass>.Instance.ProductionController.OnProductionComplete += (obj) =>
            {
                EAreaType areaType = obj.AreaType;
                if (Globals.Debug)
                {
                    string ids = string.Join(",", obj.Schemes.Values.Select(scheme => scheme._id));
                    LogHelper.LogInfoWithNotification($"{areaType} completed producing {ids}.");
                }
                this.currentlyProducing.Remove(areaType);
                this.StartProduction(areaType);
            };
        }

        /// <summary>
        /// Start new production.
        /// </summary>
        /// <param name="productionBuildStoreModel"></param>
        /// <returns>the <b>current</b> production id.</returns>
        public string AddToStack(ProductionBuildStoreModel productionBuildStoreModel, EAreaType areaType)
        {
            if (this.ProductionStack.ContainsKey(areaType) == false)
                this.ProductionStack.Add(areaType, new Stack<ProductionBuildStoreModel>());
            this.ProductionStack[areaType].Push(productionBuildStoreModel);
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification($"ProductionStack: {string.Join(",", this.ProductionStack[areaType])}");
            return this.StartProduction(areaType);
            //TODO stack in backend.
            //string json = Json.Serialize(productionBuildStoreModel);
            //ProductionBuildStoreModel deserialized = Json.Deserialize<ProductionBuildStoreModel>(json);
        }

        public ProductionBuildStoreModel GetNextProduction(EAreaType areaType)
        {
            if (this.ProductionStack.ContainsKey(areaType) == false)
                this.ProductionStack.Add(areaType, new Stack<ProductionBuildStoreModel>());
            return this.ProductionStack[areaType].Pop();
        }

        public string StartProduction(EAreaType areaType)
        {
            if (this.currentlyProducing.TryGetValue(areaType, out string nowProducingId))
            {
                if (Globals.Debug)
                    LogHelper.LogInfoWithNotification($"{areaType} already producing: {nowProducingId}");
                return string.Empty;
            }

            ProductionBuildStoreModel production = this.GetNextProduction(areaType);
            if (production == null)
            {
                if (Globals.Debug)
                    LogHelper.LogInfoWithNotification($"{areaType} stack complete done.");
                return string.Empty;
            }
            string productionId = production.Id;
            ProductionBuild productionbuild = new ProductionBuild()
            {
                Id = productionId,
                AreaType = production.AreaType,
                Continuous = production.Continuous,
                Count = production.Count,
                IsCodeProduction = production.IsCodeProduction,
                NeedFuelForAllProductionTime = production.NeedFuelForAllProductionTime,
                ProductionLimitCount = production.ProductionLimitCount,
                ProductionTime = production.ProductionTime,
                Requirements = production.Requirements
            };
            this.currentlyProducing.Add(areaType, productionId);
            //Singleton<HideoutClass>.Instance.StartContinuousProduction(productionbuild.Id); //TODO Why this function.
            Singleton<HideoutClass>.Instance.StartSingleProduction(productionbuild, delegate
            {
            });
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification($"{areaType} started producing {productionId}.");
            return productionId;
        }

        public Dictionary<EAreaType, Stack<ProductionBuildStoreModel>> ProductionStack { get; set; } = [];
    }
}