using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public class ProductionCountRequestData : IRequestData
    {
        [JsonPropertyName("area")]
        public HideoutAreas Area { get; set; }

        [JsonPropertyName("recipeId")]
        public virtual MongoId RecipeId { get; set; }
    }
}