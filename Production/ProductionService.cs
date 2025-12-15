using Comfort.Common;
using EFT;
using EFT.Hideout;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production.Requests;
using Newtonsoft.Json;
using SPT.Common.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HideoutAutomation.Production
{
    internal class ProductionService
    {
        private Dictionary<EAreaType, ProductionBuild> currentlyProducing = new Dictionary<EAreaType, ProductionBuild>();

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
                bool showItemsListWindow = false;
                var producers = Singleton<HideoutClass>.Instance.ProductionController.GetAreaProducers(obj.AreaData_0)
                                                                                    .Where(ap => ap.ProducingItems.Values.Any(pi => pi.AvailableForFinish));
                foreach (var producer in producers)
                    foreach (ProductionBuildAbstractClass productionBuild in obj.Schemes.Values)
                    {
                        string schemeId = productionBuild._id;
                        Singleton<HideoutClass>.Instance.GetProducedItems(producer, schemeId, showItemsListWindow);
                        if (Globals.Debug)
                            LogHelper.LogInfoWithNotification($"{areaType} GetProducedItems {schemeId}.");
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

        public async Task<int> GetCountInProduction(string productionId, EAreaType areaType)
        {
            ProductionCountRequest productionCountRequest = new ProductionCountRequest()
            {
                area = areaType,
                recipeId = productionId
            };
            string response = await RequestHandler.PutJsonAsync("/hideoutautomation/ProductionCount", JsonConvert.SerializeObject(productionCountRequest));
            return JsonConvert.DeserializeObject<int>(response);
            //bool isProducing = this.currentlyProducing.TryGetValue(areaType, out ProductionBuild productionbuild);
            //if (this.ProductionStack.ContainsKey(areaType) == false)
            //    return 0;
            //return this.ProductionStack[areaType].Where(p => p.Id == productionId).Sum(p => p.Count)
            //    + (isProducing ? productionbuild.Count : 0);
        }

        public int GetCountStack(string productionId, EAreaType areaType)
        {
            if (this.ProductionStack.ContainsKey(areaType) == false)
                return 0;
            return this.ProductionStack[areaType].Where(p => p.Id == productionId).Count();
        }

        public ProductionBuildStoreModel GetNextProduction(EAreaType areaType)
        {
            if (this.ProductionStack.ContainsKey(areaType) == false)
                this.ProductionStack.Add(areaType, new Stack<ProductionBuildStoreModel>());
            return this.ProductionStack[areaType].Pop();
        }

        public string StartProduction(EAreaType areaType)
        {
            if (this.currentlyProducing.TryGetValue(areaType, out ProductionBuild productionbuild))
            {
                string nowProducingId = productionbuild.Id;
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

            //TODO Check if Fuel is needed and generator is on. Else to back of productionstack?
            //production.NeedFuelForAllProductionTime;
            //Singleton<HideoutClass>.Instance.EnergyController.IsEnergyGenerationOn;

            string productionId = production.Id;
            productionbuild = new ProductionBuild()
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
            this.currentlyProducing.Add(areaType, productionbuild);
            //TODO With dialog.
            Singleton<HideoutClass>.Instance.StartSingleProduction(productionbuild, delegate
            {
                //TODO UpdateProductions
                //Singleton<HideoutClass>.Instance.ProductionController.UpdateProductions();
                AreaData areaData = Singleton<HideoutClass>.Instance.AreaDatas.FirstOrDefault(ad => ad.Template.Type == areaType);
                if (areaData == null)
                    LogHelper.LogErrorWithNotification("areaData == null");
                var producer = Singleton<HideoutClass>.Instance.ProductionController.GetAreaProducers(areaData)
                                                                     .FirstOrDefault(ap => ap.Schemes.ContainsKey(productionId));
                if (producer == null)
                    LogHelper.LogErrorWithNotification("producer == null");
                producer.StartProducing(productionbuild);
            });
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification($"{areaType} started producing {productionId}.");
            return productionId;
        }

        public Dictionary<EAreaType, Stack<ProductionBuildStoreModel>> ProductionStack { get; set; } = [];
    }
}