using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record ProductionPreservationRequestData : IRequestData
    {
        [JsonPropertyName("area")]
        public HideoutAreas Area { get; set; }

        [JsonPropertyName("recipeId")]
        public virtual MongoId RecipeId { get; set; }

        [JsonPropertyName("items")]
        public virtual List<IdWithCount>? Items { get; set; }

        [JsonPropertyName("tools")]
        public virtual List<IdWithCount>? Tools { get; set; }
    }
}