using SPTarkov.Server.Core.Models.Common;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record RecipePreservation
    {
        [JsonPropertyName("recipeId")]
        public required MongoId RecipeId { get; set; }

        [JsonPropertyName("itemPreservations")]
        public List<ItemPreservation> ItemPreservations { get; set; } = [];
    }
}