using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using System.Linq;
using System.Reflection;

namespace HideoutAutomation.Server.Patches
{
    public class MoveItem : AbstractPatch
    {
        [PatchPrefix]
        public static bool PatchPrefix(PmcData pmcData, InventoryMoveRequestData moveRequest, MongoId sessionId, ItemEventRouterResponse output)
        {
            if (ServiceLocator.ServiceProvider.GetService<InventoryHelper>() is InventoryHelper inventoryHelper
                && moveRequest.Item != null)
            {
                var ownerInventoryItems = inventoryHelper.GetOwnerInventoryItems(moveRequest, moveRequest.Item.Value, sessionId);
                if (ownerInventoryItems.SameInventory.GetValueOrDefault(false))
                {
                    // Check for item in inventory before allowing internal transfer
                    Item? originalItemLocation = ownerInventoryItems.From?.FirstOrDefault(item => item.Id == moveRequest.Item);
                    if (moveRequest.To?.Container == "hideout" && originalItemLocation is null)
                        return false;
                }
            }
            return true;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryController), nameof(InventoryController.MoveItem));
        }
    }
}