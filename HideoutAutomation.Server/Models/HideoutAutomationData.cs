using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record HideoutAutomationData
    {
        [JsonPropertyName("completedProductions")]
        public List<HideoutSingleProductionStartRequestData> CompletedProductions { get; } = [];

        [JsonPropertyName("areaProductions")]
        public Dictionary<HideoutAreas, LinkedList<HideoutSingleProductionStartRequestData>> AreaProductions { get; } = [];

        [JsonPropertyName("productionItems")]
        public List<Item> ProductionItems { get; } = [];
    }
}