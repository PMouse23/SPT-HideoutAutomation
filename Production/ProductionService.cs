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
                        LogHelper.LogInfoWithNotification($"{areaType} completed producing {completedSchemeId}.");

                    bool showItemsListWindow = false;
                    if(await Singleton<HideoutClass>.Instance.GetProducedItems(producer, completedSchemeId, showItemsListWindow))
                        producer.GetItems(completedSchemeId);
                    if (Globals.Debug)
                        LogHelper.LogInfoWithNotification($"{areaType} GetProducedItems {completedSchemeId}.");
                }
                catch (Exception ex)
                {
                    LogHelper.LogExceptionToConsole(ex);
                }
            };
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

        public int ResultCount()
        {
            //bool isProducing = this.currentlyProducing.TryGetValue(areaType, out ProductionBuild productionbuild);
            //if (this.ProductionStack.ContainsKey(areaType) == false)
            //    return 0;
            //return this.ProductionStack[areaType].Where(p => p.Id == productionId).Sum(p => p.Count)
            //    + (isProducing ? productionbuild.Count : 0);
            return 0;
        }
    }
}