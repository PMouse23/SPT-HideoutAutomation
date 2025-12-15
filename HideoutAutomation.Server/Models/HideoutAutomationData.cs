using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record HideoutAutomationData
    {
        [JsonPropertyName("areaProductions")]
        public Dictionary<HideoutAreas, List<HideoutSingleProductionStartRequestData>> AreaProductions { get; set; } = [];

        [JsonPropertyName("areaRecipePreservations")]
        public Dictionary<HideoutAreas, List<RecipePreservation>> AreaRecipePreservations { get; set; } = [];
    }
}