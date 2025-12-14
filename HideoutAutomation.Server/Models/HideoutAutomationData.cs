using SPTarkov.Server.Core.Models.Enums.Hideout;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record HideoutAutomationData
    {
        [JsonPropertyName("areaRecipePreservations")]
        public Dictionary<HideoutAreas, List<RecipePreservation>> AreaRecipePreservations { get; set; } = [];
    }
}