using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models
{
    public class FindItemsRequestData : IRequestData
    {
        [JsonPropertyName("itemIds")]
        public MongoId[]? ItemIds { get; set; }
    }
}