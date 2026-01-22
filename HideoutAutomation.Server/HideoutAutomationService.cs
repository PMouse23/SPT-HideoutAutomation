using HideoutAutomation.Server.Models;
using HideoutAutomation.Server.Stores;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;
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
        DatabaseService databaseService,
        HideoutController hideoutController,
        HideoutHelper hideoutHelper,
        InventoryHelper inventoryHelper,
        EventOutputHolder eventOutputHolder
        )
    {
        private const string purifiedWaterRecipeId = "5d5589c1f934db045e6c5492";

        public ValueTask<int> AreaCount(MongoId sessionId, AreaCountRequestData requestData)
        {
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(0);
            int count = 0;
            HideoutAreas area = requestData.Area;
            if (requestData.IncludeCurrentProduction && this.areaIsProducing(sessionId, area))
                count = 1;
            if (data.AreaProductions.TryGetValue(area, out Queue<HideoutSingleProductionStartRequestData>? value))
                count = count + value.Count();
            return ValueTask.FromResult(count);
        }

        public ValueTask<bool> CanFindAllItems(MongoId sessionId, FindItemsRequestData requestData)
        {
            if (requestData.ItemIds == null)
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

        public IEnumerable<MongoId> CompletedProductions(PmcData pmcData)
        {
            HideoutAutomationData? hideoutAutomationData = this.GetHideoutAutomationData(pmcData);
            if (hideoutAutomationData == null)
                return [];
            return hideoutAutomationData.CompletedProductions.ToArray().Select(production => production.RecipeId);
        }

        public ValueTask<HideoutProduction?> GetHideoutProduction(MongoId recipeId)
        {
            return ValueTask.FromResult(this.getHideoutProduction(recipeId));
        }

        public ValueTask<StateResponse> GetState(MongoId sessionId, StateRequestData requestData)
        {
            StateResponse stateResponse = new StateResponse();
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(stateResponse);
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(stateResponse);
            IEnumerable<IGrouping<MongoId, HideoutSingleProductionStartRequestData>> stacked = data.AreaProductions
                .SelectMany(queue => queue.Value)
                .GroupBy(production => production.RecipeId);
            foreach (var stack in stacked)
                stateResponse.StackCount.Add(stack.Key, stack.Count());
            foreach (var area in data.AreaProductions)
                stateResponse.AreaCount.Add(area.Key, area.Value.Count());
            Dictionary<MongoId, Production?>? productions = pmcData.Hideout?.Production;
            if (productions == null)
                return ValueTask.FromResult(stateResponse);
            foreach (KeyValuePair<MongoId, Production?> production in productions)
                stateResponse.Productions.Add(production.Key);
            return ValueTask.FromResult(stateResponse);
        }

        public MongoId? ProduceNext(MongoId sessionId, PmcData pmcData, HideoutAreas area)
        {
            if (pmcData.Id is not MongoId profileId)
                return null;
            HideoutAutomationData? data = this.GetHideoutAutomationData(pmcData);
            if (data == null)
                return null;
            if (data.AreaProductions.TryGetValue(area, out Queue<HideoutSingleProductionStartRequestData>? values) == false)
                return null;
            if (values.Count == 0)
                return null;
            MongoId? recipeId = this.ProduceNext(sessionId, pmcData, profileId, values);
            return recipeId;
        }

        public MongoId? ProduceNext(MongoId sessionId, PmcData pmcData, MongoId profileId, Queue<HideoutSingleProductionStartRequestData> values)
        {
            HideoutSingleProductionStartRequestData startRequestData = values.Dequeue();
            startRequestData.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (startRequestData.Tools == null || startRequestData.Items == null)
                return null;
            HideoutProduction? hideoutProduction = this.getHideoutProduction(startRequestData.RecipeId);
            if (hideoutProduction == null || hideoutProduction.Requirements == null)
                return null;
            hideoutController.SingleProductionStart(pmcData, startRequestData, sessionId);
            hideoutAutomationStore.Set(profileId);
            return startRequestData.RecipeId;
        }

        public void RemoveCompletedProductions(PmcData pmcData)
        {
            HideoutAutomationData? hideoutAutomationData = this.GetHideoutAutomationData(pmcData);
            if (hideoutAutomationData != null)
                hideoutAutomationData.CompletedProductions.Clear();
            if (pmcData.Id is not MongoId profileId)
                return;
            hideoutAutomationStore.Set(profileId);
        }

        public bool ShouldStack(MongoId sessionId, PmcData pmcData, HideoutSingleProductionStartRequestData requestData)
        {
            HideoutAreas? area = this.getHideoutArea(requestData.RecipeId);
            if (area == null)
                return false;
            if (this.areaIsProducing(pmcData, area.Value))
                return true;
            MongoId recipeId = requestData.RecipeId;

            HideoutProduction? hideoutProduction = this.getHideoutProduction(recipeId);
            if (hideoutProduction?.Continuous == true)
                return false;

            Production? production = null;
            if (this.areaIsProducingRecipe(pmcData, recipeId, out production) == false)
                return false;
            return production?.InProgress ?? false;
        }

        public ValueTask<bool> Stack(MongoId sessionId, HideoutSingleProductionStartRequestData requestData)
        {
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

            //int count = this.stackCount(data, area.Value, recipeId);
            //bool needToPayTool = count == 0;
            //if (needToPayTool)
            //    needToPayTool = this.areaIsProducingRecipe(pmcData, recipeId, out Production? production) == false;
            //if (needToPayTool == false)
            //    requestData.Tools?.Clear();

            //HACK don't hand in the tools this is trick at the moment.
            requestData.Tools?.Clear();

            this.takeItems(sessionId, pmcData, requestData);
            requestData.Items?.Clear();

            if (data.AreaProductions.ContainsKey(area.Value) == false)
                data.AreaProductions.Add(area.Value, []);
            if (data.AreaProductions.TryGetValue(area.Value, out Queue<HideoutSingleProductionStartRequestData>? value) == false)
                return ValueTask.FromResult(false);
            value.Enqueue(requestData);
            hideoutAutomationStore.Set(profileId);
            return ValueTask.FromResult(true);
        }

        public ValueTask<int> StackCount(MongoId sessionId, ProductionCountRequestData requestData)
        {
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(0);
            HideoutAreas area = requestData.Area;
            int count = this.stackCount(data, area, requestData.RecipeId);
            return ValueTask.FromResult(count);
        }

        public void StartFromStack(MongoId sessionId)
        {
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return;
            IEnumerable<HideoutAreas?> areas = databaseService.GetHideout().Areas.Select(a => a.Type);
            foreach (HideoutAreas? area in areas)
            {
                if (area is null)
                    continue;
                if (this.areaIsProducing(pmcData, area.Value))
                    continue;
                this.ProduceNext(sessionId, pmcData, area.Value);
            }
        }

        public ValueTask<HideoutProduction?> StartFromStack(MongoId sessionId, NextProductionRequestData requestData)
        {
            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return default;
            HideoutAreas area = requestData.Area;
            if (this.areaIsProducing(pmcData, area))
                return default;
            MongoId? recipeId = this.ProduceNext(sessionId, pmcData, area);
            if (recipeId == null)
                return default;
            return this.GetHideoutProduction(recipeId.Value);
        }

        public void UpdateProductionQueue(MongoId sessionId, PmcData? pmcData)
        {
            if (pmcData == null)
                return;
            HideoutAutomationData? data = this.GetHideoutAutomationData(pmcData);
            if (data == null)
                return;
            long currentTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long completionTime = currentTimeStamp;
            foreach (var areaProduction in data.AreaProductions)
            {
                double previousCraftTime = 0;
                long? startTimeStamp = null;
                MongoId? currentRecipeId = null;
                HideoutAreas area = areaProduction.Key;
                List<HideoutSingleProductionStartRequestData> openProductions = [];
                List<HideoutSingleProductionStartRequestData> completedProductions = [];
                IEnumerable<HideoutSingleProductionStartRequestData> productionsSorted = areaProduction.Value
                                                                                                        .GroupBy(p => p.RecipeId)
                                                                                                        .SelectMany(g => g.OrderBy(p => p.Timestamp));
                foreach (var production in productionsSorted)
                {
                    var recipeId = production.RecipeId;
                    if (currentRecipeId != recipeId)
                    {
                        //prefTime = 0; //Use when multiple crafts
                        //startTimeStamp = null; //Use when multiple crafts
                        currentRecipeId = recipeId;
                    }
                    double craftTime = hideoutHelper.GetAdjustedCraftTimeWithSkills(pmcData, recipeId, true).GetValueOrDefault();
                    bool isFirstInStack = startTimeStamp == null;
                    if (isFirstInStack)
                        startTimeStamp = production.Timestamp;
                    else
                    {
                        startTimeStamp += (long)previousCraftTime;
                        production.Timestamp = startTimeStamp;
                    }
                    if (startTimeStamp != null && craftTime > 0)
                        completionTime = startTimeStamp.Value + (long)craftTime;
                    if (currentTimeStamp > completionTime)
                        completedProductions.Add(production);
                    else
                        openProductions.Add(production);
                    previousCraftTime = craftTime;
                }
                data.AreaProductions[area] = new Queue<HideoutSingleProductionStartRequestData>(openProductions.OrderBy(production => production.Timestamp));
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
            return databaseService.GetHideout().Production.Recipes?.FirstOrDefault(prod => prod.Id == recipeId);
        }

        private int stackCount(HideoutAutomationData data, HideoutAreas area, MongoId recipeId)
        {
            if (data.AreaProductions.TryGetValue(area, out Queue<HideoutSingleProductionStartRequestData>? value))
                return value.Where(psd => psd.RecipeId == recipeId).Count();
            return 0;
        }

        private void takeItems(MongoId sessionId, PmcData pmcData, HideoutSingleProductionStartRequestData requestData)
        {
            List<Item>? items = pmcData.Inventory?.Items;
            if (items == null || requestData.Items == null)
                return;
            foreach (IdWithCount idWithCount in requestData.Items)
            {
                MongoId id = idWithCount.Id;
                Item? item = items.Find(i => i.Id == id);
                if (item == null)
                    continue;

                if (idWithCount.Count is double count)
                {
                    ItemEventRouterResponse output = eventOutputHolder.GetOutput(sessionId);
                    inventoryHelper.RemoveItemByCount(pmcData, id, (int)count, sessionId, output);
                }
            }
        }
    }
}