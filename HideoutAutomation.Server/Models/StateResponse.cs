using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record StateResponse
    {
        [JsonPropertyName("areaCount")]
        public Dictionary<HideoutAreas, int> AreaCount { get; init; } = [];

        [JsonPropertyName("productions")]
        public List<MongoId> Productions { get; init; } = [];

        [JsonPropertyName("stackCount")]
        public Dictionary<MongoId, int> StackCount { get; init; } = [];
    }
}