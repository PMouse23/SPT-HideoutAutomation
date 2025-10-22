using Comfort.Common;
using EFT;
using System.Collections.Generic;

namespace HideoutAutomation.Production
{
    internal class ProductionService
    {
        private Dictionary<EAreaType, string> currentlyProducing = new Dictionary<EAreaType, string>();

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
            ProductionBuildStoreModel production = this.GetNextProduction(areaType);
            if (production == null)
                return string.Empty;
            if (this.currentlyProducing.TryGetValue(areaType, out string nowProducingId))
                return nowProducingId;
            string productionId = production.Id;
            ProductionBuild productionbuild = new ProductionBuild()
            {
                Id = productionId,
                Requirements = production.Requirements
            };
            this.currentlyProducing.Add(areaType, productionId);
            Singleton<HideoutClass>.Instance.StartSingleProduction(productionbuild, delegate
            {
                this.currentlyProducing.Remove(areaType);
                this.StartProduction(areaType);
            });
            return productionId;
        }

        public Dictionary<EAreaType, Stack<ProductionBuildStoreModel>> ProductionStack { get; set; } = [];
    }
}