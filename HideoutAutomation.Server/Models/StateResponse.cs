using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record StateResponse
    {
        [JsonPropertyName("areaCount")]
        public Dictionary<HideoutAreas, int> AreaCount = [];

        [JsonPropertyName("productions")]
        public List<MongoId> Productions = [];

        [JsonPropertyName("stackCount")]
        public Dictionary<MongoId, int> StackCount = [];
    }
}