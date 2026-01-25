using Comfort.Common;
using EFT;
using EFT.Hideout;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production.Requests;
using HideoutAutomation.Production.Responses;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HideoutAutomation.Production
{
    internal class ProductionService
    {
        private readonly List<ProduceView> produceViews = [];

        private StateResponse state;

        public ProductionService()
        {
            Singleton<HideoutClass>.Instance.ProductionController.OnProductionComplete += async (obj) =>
            {
                try
                {
                    if (RaidHelper.HasRaidLoaded())
                    {
                        Singleton<ProductionService>.Instance = null;
                        return;
                    }
                    var producer = obj;
                    EAreaType areaType = producer.AreaType;
                    string completedSchemeId = producer.CompleteItemsStorage.FindCompleteItems().Item1;

                    if (isContinuousScheme(producer, completedSchemeId))
                    {
                        this.updateProduceViews();
                        return;
                    }

                    if (Globals.Debug)
                        LogHelper.LogInfoWithNotification($"{areaType} GetProducedItems {completedSchemeId}.");
                    bool showItemsListWindow = false;
                    if (this.CanFindProduction(completedSchemeId))
                        await Singleton<HideoutClass>.Instance.GetProducedItems(producer, completedSchemeId, showItemsListWindow);
                    producer.GetItems(completedSchemeId);
                    int count = this.GetAreaCount(areaType);
                    if (count > 0)
                    {
                        await Task.Delay(500);
                        if (Globals.Debug)
                            LogHelper.LogInfoWithNotification($"GetAreaCount {count}.");
                        ProductionBuild next = await this.StartFromStack(areaType);
                        if (next != null)
                        {
                            if (Globals.Debug)
                                LogHelper.LogInfoWithNotification($"StartProducing {next.Id}.");

                            var producingItem = next.GetProducingItem(producer.ProductionSpeedCoefficient, producer.ReductionCoefficient);
                            if (producingItem != null && producingItem.SchemeId != null)
                            {
                                if (Globals.Debug)
                                    LogHelper.LogInfoWithNotification($"producingItem {producingItem.SchemeId}.");
                                producer.StartProducing(next);
                            }
                        }
                    }

                    await Singleton<ProductionService>.Instance.GetState();
                    this.updateProduceViews();
                }
                catch (Exception ex)
                {
                    LogHelper.LogExceptionToConsole(ex);
                }
            };
        }

        public void AddProduceView(ProduceView produceView)
        {
            this.produceViews.Add(produceView);
        }

        public int GetAreaCount(EAreaType areaType)
        {
            bool includeCurrentProduction = false;
            return this.GetAreaCount(areaType, includeCurrentProduction);
        }

        public int GetAreaCount(EAreaType areaType, bool includeCurrentProduction)
        {
            if (Globals.Debug && this.state.areaCount == null)
                LogHelper.LogErrorWithNotification("this.state.areaCount is null.");
            int count = 0;
            this.state.areaCount?.TryGetValue(areaType, out count);
            return count;
        }

        public int GetStackCount(string productionId)
        {
            if (Globals.Debug && this.state.stackCount == null)
                LogHelper.LogErrorWithNotification("this.state.stackCount is null.");
            int productionCount = 0;
            this.state.stackCount?.TryGetValue(productionId, out productionCount);
            return productionCount;
        }

        public async Task<StateResponse> GetState()
        {
            string response = await RequestHandler.GetJsonAsync("/hideoutautomation/State");
            if (Globals.Debug)
                LogHelper.LogInfo($"state: {response}");
            StateResponse result = JsonConvert.DeserializeObject<StateResponse>(response);
            this.state = result;
            return result;
        }

        public void RemoveProduceView(ProduceView produceView)
        {
            this.produceViews.Remove(produceView);
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

        private static bool isContinuousScheme(GClass2431 producer, string completedSchemeId)
        {
            return producer.Schemes.ContainsKey(completedSchemeId) && producer.Schemes[completedSchemeId].continuous;
        }

        private bool CanFindProduction(string schemeId)
        {
            FindProductionRequest findProductionRequest = new FindProductionRequest()
            {
                schemeId = schemeId
            };
            string response = RequestHandler.PutJson("/hideoutautomation/CanFindProduction", JsonConvert.SerializeObject(findProductionRequest));
            return JsonConvert.DeserializeObject<bool>(response);
        }

        private void updateProduceViews()
        {
            foreach (ProduceView produceView in produceViews)
                produceView.UpdateView();
        }

        public StateResponse State
        {
            get
            {
                return this.state;
            }
        }
    }
}