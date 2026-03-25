using Comfort.Common;
using EFT;
using EFT.Hideout;
using HideoutAutomation.Extensions;
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
        private readonly Dictionary<string, float> schemeProductonTimes = new Dictionary<string, float>();
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
                    if (Globals.ProductionStacking == false)
                        return;

                    var producer = obj;
                    EAreaType areaType = producer.AreaType;
                    string completedSchemeId = producer.CompleteItemsStorage.FindCompleteItems().Item1;

                    if (isContinuousScheme(producer, completedSchemeId))
                    {
                        this.UpdateProduceViews();
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
                        ProductionBuild next = await this.startFromStack(areaType);
                        if (next != null)
                            Singleton<HideoutClass>.Instance.StartProducing(next);
                    }

                    await this.GetState();
                    this.UpdateProduceViews();
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

        public float CalculateProductionTime(string schemeId, Func<float> calculateProductionTimeCallback)
        {
            if (this.schemeProductonTimes.ContainsKey(schemeId))
                return this.schemeProductonTimes[schemeId];
            float producingTime = calculateProductionTimeCallback.Invoke();
            this.schemeProductonTimes.Add(schemeId, producingTime);
            return producingTime;
        }

        public int GetAreaCount(EAreaType areaType)
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

        public bool Unstack(string schemeId)
        {
            UnstackProductionRequest unstackProductionRequest = new UnstackProductionRequest()
            {
                recipeId = schemeId
            };
            string response = RequestHandler.PutJson("/hideoutautomation/Unstack", JsonConvert.SerializeObject(unstackProductionRequest));
            return JsonConvert.DeserializeObject<bool>(response);
        }

        public void UpdateProduceViews()
        {
            foreach (ProduceView produceView in produceViews)
                produceView.UpdateView();
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

        private async Task<ProductionBuild> startFromStack(EAreaType areaType)
        {
            NextProductionRequest nextProductionRequest = new NextProductionRequest()
            {
                area = areaType,
            };
            string response = await RequestHandler.PutJsonAsync("/hideoutautomation/StartFromStack", JsonConvert.SerializeObject(nextProductionRequest));
            return JsonConvert.DeserializeObject<ProductionBuild>(response);
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