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
        public List<HideoutSingleProductionStartRequestData> CompletedProductions { get; set; } = [];

        [JsonPropertyName("areaProductions")]
        public Dictionary<HideoutAreas, Queue<HideoutSingleProductionStartRequestData>> AreaProductions { get; set; } = [];

        [JsonPropertyName("productionItems")]
        public List<Item> ProductionItems { get; set; } = [];
    }
}