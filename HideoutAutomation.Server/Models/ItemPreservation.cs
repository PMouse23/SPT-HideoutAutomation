using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public record ItemPreservation
    {
        [JsonPropertyName("templateId")]
        public required MongoId TemplateId { get; set; }

        [JsonPropertyName("count")]
        public required double Count { get; set; }
    }
}