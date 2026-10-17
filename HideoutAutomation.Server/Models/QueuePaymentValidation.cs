using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;

namespace HideoutAutomation.Server.Models;

public static class QueuePaymentValidation
{
    public static bool IsValid(PmcData profile, HideoutSingleProductionStartRequestData request, HideoutProduction? recipe)
    {
        if (recipe?.Requirements == null || recipe.Continuous == true || request.Items == null || request.Tools == null)
            return false;
        var inventory = profile.Inventory?.Items;
        if (inventory == null) return false;
        var paid = new Dictionary<MongoId, double>();
        var ids = new HashSet<MongoId>();
        foreach (var payment in request.Items)
        {
            if (!ids.Add(payment.Id) || payment.Count is not double count || !double.IsFinite(count)
                || count <= 0 || count != Math.Truncate(count) || count > int.MaxValue)
                return false;
            var item = inventory.FirstOrDefault(candidate => candidate.Id == payment.Id);
            if (item == null || count > (item.Upd?.StackObjectsCount ?? 1)) return false;
            paid[item.Template] = paid.GetValueOrDefault(item.Template) + count;
        }
        if (recipe.Requirements.Any(requirement => requirement.Type == "Item" && requirement.Count > 0 && requirement.TemplateId == null))
            return false;
        var required = recipe.Requirements.Where(requirement => requirement.Type == "Item" && requirement.Count > 0)
            .GroupBy(requirement => requirement.TemplateId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(requirement => requirement.Count ?? 0));
        return paid.Count == required.Count && required.All(pair => paid.TryGetValue(pair.Key, out var count) && count == pair.Value);
    }
}
