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
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HideoutAutomation.Server
{
    [Injectable(InjectionType = InjectionType.Singleton)]
    public class HideoutAutomationService(
        ISptLogger<HideoutAutomationService> logger,
        ProfileHelper profileHelper,
        HideoutAutomationStore hideoutAutomationStore,
        HideoutController hideoutController,
        InventoryHelper inventoryHelper,
        EventOutputHolder eventOutputHolder
        )
    {
        public ValueTask<int> AreaCount(MongoId sessionId, ProductionCountRequestData requestData)
        {
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(0);
            int count = 0;
            HideoutAreas area = requestData.Area;
            if (data.AreaProductions.ContainsKey(area))
                count = data.AreaProductions[area].Count;
            return ValueTask.FromResult(count);
        }

        public void Log(string data)
        {
            logger.Success(data);
        }

        public ValueTask<bool> MakePreservation(MongoId sessionId, ProductionPreservationRequestData requestData)
        {
            logger.Success($"HideoutAutomation: MakePreservation {sessionId}");

            PmcData? pmcData = profileHelper.GetPmcProfile(sessionId);
            if (pmcData == null)
                return ValueTask.FromResult(false);
            if (pmcData.Id is not MongoId profileId)
                return ValueTask.FromResult(false);

            HideoutAutomationData data = hideoutAutomationStore.Get(profileId);
            List<Item>? items = pmcData.Inventory?.Items;
            if (items == null)
                return ValueTask.FromResult(false);

            HideoutAreas area = requestData.Area;
            RecipePreservation recipePreservation = new RecipePreservation()
            {
                RecipeId = requestData.RecipeId
            };
            foreach (IdWithCount idWithCount in requestData.Items)
            {
                MongoId id = idWithCount.Id;
                Item? item = items.Find(i => i.Id == id);
                if (item == null)
                {
                    logger.Error($"HideoutAutomation: Cannot find item {id}");
                    return ValueTask.FromResult(false);
                }
                if (idWithCount.Count is double count)
                {
                    recipePreservation.ItemPreservations.Add(new ItemPreservation() { TemplateId = id, Count = count });
                    ItemEventRouterResponse output = eventOutputHolder.GetOutput(sessionId);
                    inventoryHelper.RemoveItemByCount(pmcData, id, (int)count, sessionId, output);
                }
            }
            if (data.AreaRecipePreservations.ContainsKey(area) == false)
                data.AreaRecipePreservations.Add(area, [recipePreservation]);
            else
                data.AreaRecipePreservations[area].Add(recipePreservation);
            hideoutAutomationStore.Set(profileId);
            logger.Success($"HideoutAutomation: Made preservation for recipe {recipePreservation.RecipeId}");
            return ValueTask.FromResult(true);
        }

        public ValueTask<int> StackCount(MongoId sessionId, ProductionCountRequestData requestData)
        {
            HideoutAutomationData? data = this.GetHideoutAutomationData(sessionId);
            if (data == null)
                return ValueTask.FromResult(0);
            int count = 0;
            HideoutAreas area = requestData.Area;
            if (data.AreaProductions.TryGetValue(area, out List<HideoutSingleProductionStartRequestData>? value))
                count = value.Where(psd => psd.RecipeId == requestData.RecipeId).Count();
            return ValueTask.FromResult(count);
        }

        public void StartProducing(PmcData pmcData, HideoutSingleProductionStartRequestData requestData, MongoId sessionID)
        {
            hideoutController.SingleProductionStart(pmcData, requestData, sessionID);
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
    }
}