using SPTarkov.Server.Core.Models.Common;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public class StateResponse
    {
        [JsonPropertyName("productions")]
        public List<MongoId> Productions = [];

        [JsonPropertyName("stackCount")]
        public Dictionary<MongoId, int> StackCount = [];
    }
}