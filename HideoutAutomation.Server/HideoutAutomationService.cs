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
        private HideoutSingleProductionStartRequestData? last;

        public void Log(string data)
        {
            logger.Success(data);
        }

        public ValueTask<bool> MakePreservation(MongoId sessionId, ProductionPreservationRequestData request)
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

            HideoutAreas area = request.Area;
            RecipePreservation recipePreservation = new RecipePreservation()
            {
                RecipeId = request.RecipeId
            };
            foreach (IdWithCount idWithCount in request.Items)
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

        public void SetLast(HideoutSingleProductionStartRequestData last)
        {
            this.last = last;
        }

        public void StartProducing(PmcData pmcData, MongoId sessionID)
        {
            if (this.last == null)
                return;
            hideoutController.SingleProductionStart(pmcData, this.last, sessionID);
        }
    }
}