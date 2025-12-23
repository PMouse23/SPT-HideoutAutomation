using HideoutAutomation.Server.Models;
using HideoutAutomation.Server.Stores;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
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
        HideoutController hideoutController
        )
    {
        public ValueTask<HideoutProduction?> GetHideoutProduction(MongoId recipeId)
        {
            return ValueTask.FromResult(this.getHideoutProduction(recipeId));
        }

        public MongoId? ProduceNext(MongoId sessionId, PmcData pmcData, HideoutAreas area)
        {
            if (pmcData.Id is not MongoId profileId)
                return null;
            HideoutAutomationData? data = this.GetHideoutAutomationData(pmcData);
            if (data == null)
                return null;
            if (data.AreaProductions.TryGetValue(area, out Stack<HideoutSingleProductionStartRequestData>? values) == false)
                return null;
            if (values.Count == 0)
                return null;
            MongoId recipeId = this.ProduceNext(sessionId, pmcData, profileId, values);
            return recipeId;
        }

        public MongoId ProduceNext(MongoId sessionId, PmcData pmcData, MongoId profileId, Stack<HideoutSingleProductionStartRequestData> values)
        {
            HideoutSingleProductionStartRequestData startRequestData = values.Pop();
            hideoutController.SingleProductionStart(pmcData, startRequestData, sessionId);
            hideoutAutomationStore.Set(profileId);
            return startRequestData.RecipeId;
        }

        public bool ShouldStack(MongoId sessionId, PmcData pmcData, HideoutSingleProductionStartRequestData requestData)
        {
            HideoutAreas? area = this.getHideoutArea(requestData.RecipeId);
            if (area == null)
                return false;
            if (this.areaIsProducing(pmcData, area.Value))
                return true;
            MongoId recipeId = requestData.RecipeId;
            Production? production = null;
            if (pmcData.Hideout?.Production?.TryGetValue(recipeId, out production) == false)
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
            HideoutAreas? area = this.getHideoutArea(requestData.RecipeId);
            if (area == null)
                return ValueTask.FromResult(false);
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(false);
            if (data.AreaProductions.ContainsKey(area.Value) == false)
                data.AreaProductions.Add(area.Value, []);
            if (data.AreaProductions.TryGetValue(area.Value, out Stack<HideoutSingleProductionStartRequestData>? value) == false)
                return ValueTask.FromResult(false);
            value.Push(requestData);
            hideoutAutomationStore.Set(profileId);
            return ValueTask.FromResult(true);
        }

        public ValueTask<int> StackCount(MongoId sessionId, ProductionCountRequestData requestData)
        {
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(0);
            int count = 0;
            HideoutAreas area = requestData.Area;
            if (data.AreaProductions.TryGetValue(area, out Stack<HideoutSingleProductionStartRequestData>? value))
                count = value.Where(psd => psd.RecipeId == requestData.RecipeId).Count();
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

        private bool areaIsProducing(PmcData pmcData, HideoutAreas area)
        {
            IEnumerable<Production?> productions = this.getAreaProductions(pmcData, area);
            if (productions.Count() > 0)
                return true;
            return false;
        }

        private IEnumerable<Production?> getAreaProductions(PmcData pmcData, HideoutAreas area)
        {
            return pmcData.Hideout?.Production?.Values?.Where(prod => prod != null && this.getHideoutArea(prod.RecipeId) == area) ?? [];
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
    }
}