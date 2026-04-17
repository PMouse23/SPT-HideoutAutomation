using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record ProductionStartRequestAndPaymentData
    {
        [JsonPropertyName("requestData")]
        public required HideoutSingleProductionStartRequestData RequestData { get; set; }

        [JsonPropertyName("paymentItems")]
        public List<Item> PaymentItems { get; set; } = [];
    }
}