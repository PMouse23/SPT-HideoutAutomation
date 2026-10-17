using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record HideoutAutomationData
    {
        [JsonPropertyName("completedProductions")]
        public List<HideoutSingleProductionStartRequestData> CompletedProductions { get; set; } = [];

        // Legacy queue data is retained for migration.
        [JsonPropertyName("areaProductions")]
        public Dictionary<HideoutAreas, LinkedList<HideoutSingleProductionStartRequestData>> AreaProductions { get; set; } = [];

        [JsonPropertyName("areaProductionsAndPayment")]
        public Dictionary<HideoutAreas, LinkedList<ProductionStartRequestAndPaymentData>> AreaProductionsAndPayment { get; set; } = [];
    }
}