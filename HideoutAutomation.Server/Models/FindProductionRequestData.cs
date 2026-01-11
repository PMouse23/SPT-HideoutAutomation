using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public class FindProductionRequestData : IRequestData
    {
        [JsonPropertyName("schemeId")]
        public MongoId? SchemeId { get; set; }
    }
}