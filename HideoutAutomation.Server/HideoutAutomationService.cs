using HideoutAutomation.Server.Models;
using HideoutAutomation.Server.Stores;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Ragfair;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils.Cloners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(InjectionType = InjectionType.Singleton)]
    public class HideoutAutomationService(
        ProfileHelper profileHelper,
        HideoutAutomationStore hideoutAutomationStore,
        SPTarkov.Server.Core.Models.Spt.Tables.HideoutTable hideoutTable,
        HideoutController hideoutController,
        HideoutHelper hideoutHelper,
        InventoryHelper inventoryHelper,
        RagfairServerHelper ragfairServerHelper,
        EventOutputHolder eventOutputHolder,
        ICloner cloner
        )
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<MongoId, bool> enabledSessions = new();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public ValueTask<bool> SetQueueEnabled(MongoId sessionId, bool enabled)
        {
            enabledSessions[sessionId] = enabled;
            return ValueTask.FromResult(true);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public bool IsQueueEnabled(MongoId sessionId) => enabledSessions.TryGetValue(sessionId, out var enabled) && enabled;

        private const string purifiedWaterRecipeId = "5d5589c1f934db045e6c5492";

        private const int TIMESTAMPMARGIN = 10;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public HideoutSingleProductionStartRequestData[] GetCompletedRequests(PmcData profile) =>
            GetHideoutAutomationData(profile)?.CompletedProductions.ToArray() ?? [];

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void AcknowledgeCompleted(PmcData profile, HideoutSingleProductionStartRequestData[] requests)
        {
            var data = GetHideoutAutomationData(profile);
            if (data == null || profile.Id is not MongoId profileId) return;
            data.CompletedProductions.RemoveAll(request => requests.Any(pending => ReferenceEquals(pending, request)));
            hideoutAutomationStore.Set(profileId);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public ValueTask<bool> CanFindAllItems(MongoId sessionId, FindItemsRequestData requestData)
        {
            if (requestData.ItemIds == null || requestData.ItemIds.Length == 0)
                return ValueTask.FromResult(false);
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(false);
            foreach (MongoId itemId in requestData.ItemIds)
            {
                bool? found = pmcData.Inventory?.Items?.Any(i => i.Id == itemId);
                if (found != true)
                    return ValueTask.FromResult(false);
            }
            return ValueTask.FromResult(true);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public ValueTask<bool> CanFindProduction(MongoId sessionId, FindProductionRequestData requestData)
        {
            if (requestData.SchemeId == null)
                return ValueTask.FromResult(false);
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(false);
            bool? hasProduction = pmcData.Hideout?.Production?.Values.Any(productionInProfile => productionInProfile != null && productionInProfile.RecipeId == requestData.SchemeId);
            if (hasProduction == null)
                return ValueTask.FromResult(false);
            return ValueTask.FromResult(hasProduction.Value);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public IEnumerable<MongoId> CompletedProductions(PmcData pmcData)
        {
            HideoutAutomationData? hideoutAutomationData = this.GetHideoutAutomationData(pmcData);
            if (hideoutAutomationData == null)
                return [];
            return hideoutAutomationData.CompletedProductions.ToArray().Select(production => production.RecipeId);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public HideoutProduction? GetHideoutProduction(MongoId recipeId)
        {
            return this.getHideoutProduction(recipeId);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public ValueTask<StateResponse> GetState(MongoId sessionId)
        {
            StateResponse stateResponse = new StateResponse();
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(stateResponse);
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(stateResponse);
            IEnumerable<IGrouping<MongoId, ProductionStartRequestAndPaymentData>> stacked = data.AreaProductionsAndPayment
                .SelectMany(queue => queue.Value)
                .GroupBy(production => production.RequestData.RecipeId);
            foreach (var stack in stacked)
                stateResponse.StackCount.Add(stack.Key, stack.Count());
            foreach (var area in data.AreaProductionsAndPayment)
            {
                bool areaIsProducing = this.areaIsProducing(sessionId, area.Key);
                int areaCount = areaIsProducing ? 1 : 0;
                areaCount += area.Value.Count();
                stateResponse.AreaCount.Add(area.Key, areaCount);
            }
            Dictionary<MongoId, Production?>? productions = pmcData.Hideout?.Production;
            if (productions == null)
                return ValueTask.FromResult(stateResponse);
            foreach (var area in hideoutTable.Areas)
            {
                if (area.Type is HideoutAreas type && !stateResponse.AreaCount.ContainsKey(type) && areaIsProducing(pmcData, type))
                    stateResponse.AreaCount[type] = 1;
            }
            foreach (KeyValuePair<MongoId, Production?> production in productions)
                stateResponse.Productions.Add(production.Key);
            return ValueTask.FromResult(stateResponse);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void MoveOldtoNewQueue(MongoId sessionId, PmcData? pmcData)
        {
            if (pmcData == null)
                return;
            HideoutAutomationData? data = this.GetHideoutAutomationData(pmcData);
            if (data == null)
                return;
            if (data.AreaProductions.Any() == false)
                return;
            foreach (KeyValuePair<HideoutAreas, LinkedList<HideoutSingleProductionStartRequestData>> oldKvp in data.AreaProductions)
            {
                HideoutAreas areas = oldKvp.Key;
                foreach (HideoutSingleProductionStartRequestData requestData in oldKvp.Value)
                    this.Stack(sessionId, requestData);
            }
            data.AreaProductions.Clear();
            if (pmcData.Id is MongoId profileId)
                hideoutAutomationStore.Set(profileId);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public HideoutSingleProductionStartRequestData? ProduceNext(MongoId sessionId, PmcData pmcData, HideoutAreas area)
        {
            if (pmcData.Id is not MongoId profileId)
                return null;
            HideoutAutomationData? data = this.GetHideoutAutomationData(pmcData);
            if (data == null)
                return null;
            if (data.AreaProductionsAndPayment.TryGetValue(area, out LinkedList<ProductionStartRequestAndPaymentData>? values) == false)
                return null;
            if (values.Count == 0)
                return null;
            HideoutSingleProductionStartRequestData? started = this.ProduceNext(sessionId, pmcData, profileId, values, data);
            return started;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public HideoutSingleProductionStartRequestData? ProduceNext(MongoId sessionId, PmcData pmcData, MongoId profileId, LinkedList<ProductionStartRequestAndPaymentData> values, HideoutAutomationData data)
        {
            HideoutSingleProductionStartRequestData? startRequestData = cloner.Clone(values.FirstOrDefault()?.RequestData);
            if (startRequestData == null)
                return null;

            if (startRequestData.Items != null)
                startRequestData.Items.Clear();
            long currentTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (startRequestData.Timestamp > currentTimeStamp + TIMESTAMPMARGIN)
                startRequestData.Timestamp = currentTimeStamp;
            if (startRequestData.Tools == null || startRequestData.Items == null)
                return null;
            HideoutProduction? hideoutProduction = this.getHideoutProduction(startRequestData.RecipeId);
            if (hideoutProduction == null || hideoutProduction.Requirements == null)
                return null;
            var output = hideoutController.SingleProductionStart(pmcData, startRequestData, sessionId);
            if (output.Warnings?.Count > 0) return null;
            values.RemoveFirst();
            hideoutAutomationStore.Set(profileId);
            return startRequestData;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void RemoveCompletedProductions(PmcData pmcData)
        {
            HideoutAutomationData? hideoutAutomationData = this.GetHideoutAutomationData(pmcData);
            if (hideoutAutomationData != null)
                hideoutAutomationData.CompletedProductions.Clear();
            if (pmcData.Id is not MongoId profileId)
                return;
            hideoutAutomationStore.Set(profileId);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public bool ShouldStack(MongoId sessionId, PmcData pmcData, HideoutSingleProductionStartRequestData requestData)
        {
            if (!IsQueueEnabled(sessionId)) return false;
            HideoutAreas? area = this.getHideoutArea(requestData.RecipeId);
            if (area == null)
                return false;
            MongoId recipeId = requestData.RecipeId;
            HideoutProduction? hideoutProduction = this.getHideoutProduction(recipeId);
            if (hideoutProduction == null || hideoutProduction.Continuous == true) return false;
            if (this.areaIsProducing(pmcData, area.Value)) return true;

            Production? production = null;
            if (this.areaIsProducingRecipe(pmcData, recipeId, out production) == false)
                return false;
            return production?.InProgress ?? false;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public ValueTask<bool> Stack(MongoId sessionId, HideoutSingleProductionStartRequestData requestData)
        {
            if (!IsQueueEnabled(sessionId)) return ValueTask.FromResult(false);
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(false);
            if (pmcData.Id is not MongoId profileId)
                return ValueTask.FromResult(false);
            MongoId recipeId = requestData.RecipeId;
            HideoutAreas? area = this.getHideoutArea(recipeId);
            if (area == null)
                return ValueTask.FromResult(false);
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(false);

            if (!QueuePaymentValidation.IsValid(pmcData, requestData, getHideoutProduction(recipeId)))
                return ValueTask.FromResult(false);

            // Tools remain reusable; consumable ingredients are reserved at enqueue time.
            requestData.Tools?.Clear();

            IEnumerable<Item> items = this.takeItems(sessionId, pmcData, requestData, data);
            if (data.AreaProductionsAndPayment.ContainsKey(area.Value) == false)
                data.AreaProductionsAndPayment.Add(area.Value, []);
            if (data.AreaProductionsAndPayment.TryGetValue(area.Value, out LinkedList<ProductionStartRequestAndPaymentData>? value) == false)
                return ValueTask.FromResult(false);
            value.AddLast(new ProductionStartRequestAndPaymentData()
            {
                RequestData = requestData,
                PaymentItems = [.. items]
            });
            hideoutAutomationStore.Set(profileId);
            return ValueTask.FromResult(true);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void StartFromStack(MongoId sessionId)
        {
            if (!IsQueueEnabled(sessionId)) return;
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return;
            IEnumerable<HideoutAreas?> areas = hideoutTable.Areas.Select(a => a.Type);
            foreach (HideoutAreas? area in areas)
            {
                if (area is null)
                    continue;
                if (this.areaIsProducing(pmcData, area.Value))
                    continue;
                this.ProduceNext(sessionId, pmcData, area.Value);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public ValueTask<HideoutProduction> StartFromStack(MongoId sessionId, NextProductionRequestData requestData)
        {
            if (!IsQueueEnabled(sessionId)) return default;
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return default;
            HideoutAreas area = requestData.Area;
            if (this.areaIsProducing(pmcData, area))
                return default;
            HideoutSingleProductionStartRequestData? started = this.ProduceNext(sessionId, pmcData, area);
            if (started == null)
                return default;
            MongoId recipeId = started.RecipeId;
            HideoutProduction? hideoutProduction = this.GetHideoutProduction(recipeId);
            hideoutProduction = cloner.Clone(hideoutProduction);
            if (hideoutProduction == null)
                return default;
            long currentTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long productionTimeAsLong = 0L;
            if (hideoutProduction.ProductionTime != null)
                productionTimeAsLong = (long)hideoutProduction.ProductionTime;
            long completiontime = (started.Timestamp ?? currentTimeStamp) + productionTimeAsLong;
            double productionTime = (double)(completiontime - (started.Timestamp ?? currentTimeStamp));
            hideoutProduction.ProductionTime = productionTime > TIMESTAMPMARGIN ? productionTime : TIMESTAMPMARGIN;
            return ValueTask.FromResult(hideoutProduction);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public ValueTask<bool> Unstack(MongoId sessionId, UnstackProductionRequestData requestData)
        {
            MongoId recipeId = requestData.RecipeId;
            HideoutAreas? area = this.getHideoutArea(recipeId);
            if (area == null)
                return ValueTask.FromResult(false);
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(false);
            if (data.AreaProductionsAndPayment.TryGetValue(area.Value, out LinkedList<ProductionStartRequestAndPaymentData>? values) == false)
                return ValueTask.FromResult(false);
            ProductionStartRequestAndPaymentData? unstacked = values.LastOrDefault(production => production.RequestData.RecipeId == recipeId);
            if (unstacked == null)
                return ValueTask.FromResult(false);
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(false);
            if (pmcData.Id is not MongoId profileId)
                return ValueTask.FromResult(false);
            List<Item> itemsToReturn = unstacked.PaymentItems;
            ragfairServerHelper.ReturnItems(sessionId, itemsToReturn);
            values.Remove(unstacked);
            hideoutAutomationStore.Set(profileId);
            return ValueTask.FromResult(true);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void UpdateProductionQueue(MongoId sessionId, PmcData? pmcData)
        {
            if (!IsQueueEnabled(sessionId)) return;
            if (pmcData == null)
                return;
            HideoutAutomationData? data = this.GetHideoutAutomationData(pmcData);
            if (data == null)
                return;
            long currentTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long completionTime = currentTimeStamp;
            foreach (var areaProduction in data.AreaProductionsAndPayment.ToArray())
            {
                double previousCraftTime = 0;
                long? startTimeStamp = null;

                HideoutAreas area = areaProduction.Key;
                List<ProductionStartRequestAndPaymentData> openProductions = [];
                List<HideoutSingleProductionStartRequestData> completedProductions = [];
                IEnumerable<ProductionStartRequestAndPaymentData> productionsSorted = areaProduction.Value;
                IEnumerable<Production?> productions = this.getAreaProductions(pmcData, area);
                Production? currentProduction = productions.FirstOrDefault();
                if (currentProduction != null && currentProduction.ProductionTime is double productionTime)
                    startTimeStamp = currentProduction.StartTimestamp + (long)productionTime;
                foreach (var production in productionsSorted)
                {
                    var recipeId = production.RequestData.RecipeId;
                    double craftTime = hideoutHelper.GetAdjustedCraftTimeWithSkills(pmcData, recipeId, true).GetValueOrDefault();
                    bool isFirstInStack = startTimeStamp == null;
                    if (isFirstInStack)
                        startTimeStamp = production.RequestData.Timestamp;
                    else
                    {
                        startTimeStamp += (long)previousCraftTime;
                        production.RequestData.Timestamp = startTimeStamp;
                    }
                    if (startTimeStamp != null && craftTime > 0)
                        completionTime = startTimeStamp.Value + (long)craftTime;
                    if (currentTimeStamp > completionTime)
                        completedProductions.Add(production.RequestData);
                    else
                        openProductions.Add(production);
                    previousCraftTime = craftTime;
                }
                data.AreaProductionsAndPayment[area] = new LinkedList<ProductionStartRequestAndPaymentData>(openProductions.OrderBy(production => production.RequestData.Timestamp));
                if (completedProductions.Any() && data.CompletedProductions != null)
                    data.CompletedProductions.AddRange(completedProductions);
            }
            if (pmcData.Id is MongoId profileId)
                hideoutAutomationStore.Set(profileId);
        }

        private bool areaIsProducing(MongoId sessionId, HideoutAreas area)
        {
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return false;
            return this.areaIsProducing(pmcData, area);
        }

        private bool areaIsProducing(PmcData pmcData, HideoutAreas area)
        {
            IEnumerable<Production?> productions = this.getAreaProductions(pmcData, area);
            if (productions.Count() > 0)
                return true;
            return false;
        }

        private bool areaIsProducingRecipe(PmcData pmcData, MongoId recipeId, out Production? production)
        {
            production = null;
            bool? result = pmcData.Hideout?.Production?.TryGetValue(recipeId, out production);
            return result ?? false;
        }

        private IEnumerable<Production?> getAreaProductions(PmcData pmcData, HideoutAreas area)
        {
            return pmcData.Hideout?.Production?.Values?.Where(prod => prod != null
                                                                   && (prod.SptIsContinuous == null
                                                                   || prod.SptIsContinuous == false)
                                                                   && prod.RecipeId != purifiedWaterRecipeId
                                                                   && this.getHideoutArea(prod.RecipeId) == area) ?? [];
        }

        private HideoutAreas? getHideoutArea(MongoId recipeId)
        {
            return this.getHideoutProduction(recipeId)?.AreaType;
        }

        private HideoutAutomationData? GetHideoutAutomationData(PmcData pmcData)
        {
            if (pmcData.Id is not MongoId profileId)
                return null;
            HideoutAutomationData data = hideoutAutomationStore.Get(profileId);
            return data;
        }

        private HideoutAutomationData? GetHideoutAutomationData(MongoId sessionId)
        {
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return null;
            return this.GetHideoutAutomationData(pmcData);
        }

        private HideoutProduction? getHideoutProduction(MongoId recipeId)
        {
            return hideoutTable.Production.Recipes?.FirstOrDefault(prod => prod.Id == recipeId);
        }

        private IEnumerable<Item> takeItems(MongoId sessionId, PmcData pmcData, HideoutSingleProductionStartRequestData requestData, HideoutAutomationData data)
        {
            List<Item>? items = pmcData.Inventory?.Items;
            if (items == null || requestData.Items == null)
                yield break;
            foreach (IdWithCount idWithCount in requestData.Items)
            {
                MongoId id = idWithCount.Id;
                Item? item = items.Find(i => i.Id == id);
                if (item == null)
                    continue;

                Item? clone = cloner.Clone(item);
                if (idWithCount.Count is double count)
                {
                    ItemEventRouterResponse output = eventOutputHolder.GetOutput(sessionId);
                    inventoryHelper.RemoveItemByCount(pmcData, id, (int)count, sessionId, output);
                    if (clone?.Upd != null)
                        clone.Upd.StackObjectsCount = count;
                }
                if (clone != null)
                    yield return clone;
            }
        }
    }
}
