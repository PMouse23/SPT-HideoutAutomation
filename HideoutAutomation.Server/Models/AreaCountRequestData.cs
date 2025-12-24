using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public class AreaCountRequestData : IRequestData
    {
        [JsonPropertyName("includeCurrentProduction")]
        public bool IncludeCurrentProduction;

        [JsonPropertyName("area")]
        public HideoutAreas Area { get; set; }
    }
}