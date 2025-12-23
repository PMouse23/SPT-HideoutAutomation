using Comfort.Common;
using EFT;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production.Requests;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Threading.Tasks;

namespace HideoutAutomation.Production
{
    internal class ProductionService
    {
        public ProductionService()
        {
            Singleton<HideoutClass>.Instance.ProductionController.OnProductionComplete += async (obj) =>
            {
                try
                {
                    var producer = obj;
                    EAreaType areaType = producer.AreaType;
                    string completedSchemeId = producer.CompleteItemsStorage.FindCompleteItems().Item1;
                    if (Globals.Debug)
                        LogHelper.LogInfoWithNotification($"{areaType} GetProducedItems {completedSchemeId}.");
                    bool showItemsListWindow = false;
                    if (await Singleton<HideoutClass>.Instance.GetProducedItems(producer, completedSchemeId, showItemsListWindow))
                        producer.GetItems(completedSchemeId);

                    int count = await this.GetAreaCount(areaType);
                    if (count > 0)
                    {
                        if (Globals.Debug)
                            LogHelper.LogInfoWithNotification($"GetAreaCount {count}.");
                        ProductionBuild next = await this.StartFromStack(areaType);
                        if (Globals.Debug)
                            LogHelper.LogInfoWithNotification($"StartProducing {next.Id}.");
                        producer.StartProducing(next);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogExceptionToConsole(ex);
                }
            };
        }

        public async Task<int> GetAreaCount(EAreaType areaType)
        {
            AreaCountRequest productionCountRequest = new AreaCountRequest()
            {
                area = areaType
            };
            string response = await RequestHandler.PutJsonAsync("/hideoutautomation/AreaCount", JsonConvert.SerializeObject(productionCountRequest));
            return JsonConvert.DeserializeObject<int>(response);
        }

        public async Task<int> GetStackCount(string productionId, EAreaType areaType)
        {
            ProductionCountRequest productionCountRequest = new ProductionCountRequest()
            {
                area = areaType,
                recipeId = productionId
            };
            string response = await RequestHandler.PutJsonAsync("/hideoutautomation/StackCount", JsonConvert.SerializeObject(productionCountRequest));
            return JsonConvert.DeserializeObject<int>(response);
        }

        public async Task<ProductionBuild> StartFromStack(EAreaType areaType)
        {
            NextProductionRequest nextProductionRequest = new NextProductionRequest()
            {
                area = areaType,
            };
            string response = await RequestHandler.PutJsonAsync("/hideoutautomation/StartFromStack", JsonConvert.SerializeObject(nextProductionRequest));
            return JsonConvert.DeserializeObject<ProductionBuild>(response);
        }
    }
}