using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record UnstackProductionRequestData : IRequestData
    {
        [JsonPropertyName("recipeId")]
        public virtual MongoId RecipeId { get; set; }
    }
}