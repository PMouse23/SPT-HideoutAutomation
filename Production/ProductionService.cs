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
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace HideoutAutomation.Production
{
    internal class ProductionService : IDisposable
    {
        private readonly List<ProduceView> produceViews = [];
        private readonly HashSet<EAreaType> collecting = new();
        private readonly Action<ItemsProducerBase> onProductionComplete;
        public HideoutRepresentation Owner { get; }
        private StateResponse state = new StateResponse() { areaCount = [], productions = [], stackCount = [] };

        public ProductionService(HideoutRepresentation owner)
        {
            Owner = owner;
            onProductionComplete = async (obj) =>
            {
                if (!collecting.Add(obj.AreaType)) return;
                try
                {
                    if (RaidHelper.HasRaidLoaded())
                    {
                        this.produceViews.Clear();
                        return;
                    }
                    if (Globals.ProductionStacking == false || !ReferenceEquals(Owner, Singleton<HideoutRepresentation>.Instance))
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
                    if (!this.CanFindProduction(completedSchemeId) || !await Singleton<HideoutRepresentation>.Instance.GetProducedItems(producer, completedSchemeId, showItemsListWindow))
                        return;
                    if (!ReferenceEquals(Owner, Singleton<HideoutRepresentation>.Instance)) return;
                    producer.GetItems(completedSchemeId);
                    int count = this.GetAreaCount(areaType);
                    if (count > 0)
                    {
                        await Task.Delay(500);
                        if (Globals.Debug)
                            LogHelper.LogInfoWithNotification($"GetAreaCount {count}.");
                        if (!ReferenceEquals(Owner, Singleton<HideoutRepresentation>.Instance)) return;
                        ProductionBuild? next = await this.startFromStack(areaType);
                        if (next != null)
                            Singleton<HideoutRepresentation>.Instance.StartProducing(next);
                    }

                    await this.GetState();
                    this.UpdateProduceViews();
                }
                catch (Exception ex)
                {
                    LogHelper.LogExceptionToConsole(ex);
                }
                finally { collecting.Remove(obj.AreaType); }
            };
            Owner.ProductionController.OnProductionComplete += onProductionComplete;
        }

        public void Dispose()
        {
            Owner.ProductionController.OnProductionComplete -= onProductionComplete;
            produceViews.Clear();
        }

        public async Task SetEnabled(bool enabled)
        {
            await RequestHandler.PutJsonAsync("/hideoutautomation/QueueEnabled", JsonConvert.SerializeObject(new { enabled }));
            if (!enabled) return;
            await GetState();
            foreach (var area in state.areaCount.Keys.ToArray())
            {
                if (!ReferenceEquals(Owner, Singleton<HideoutRepresentation>.Instance)) return;
                var next = await startFromStack(area);
                if (next != null) Owner.StartProducing(next);
            }
        }

        public void AddProduceView(ProduceView produceView)
        {
            if (!this.produceViews.Contains(produceView)) this.produceViews.Add(produceView);
        }

        public void AddUpAreaCount(EAreaType areaType)
        {
            int areaCount = this.GetAreaCount(areaType);
            this.state.areaCount[areaType] = areaCount + 1;
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

        public async Task<bool> Unstack(string schemeId)
        {
            UnstackProductionRequest unstackProductionRequest = new UnstackProductionRequest()
            {
                recipeId = schemeId
            };
            string response = await RequestHandler.PutJsonAsync("/hideoutautomation/Unstack", JsonConvert.SerializeObject(unstackProductionRequest));
            return JsonConvert.DeserializeObject<bool>(response);
        }

        public void UpdateProduceViews()
        {
            foreach (ProduceView produceView in produceViews.ToArray())
                if (produceView != null) produceView.UpdateView();
        }

        private static bool isContinuousScheme(ItemsProducerBase producer, string completedSchemeId)
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

        private async Task<ProductionBuild?> startFromStack(EAreaType areaType)
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
