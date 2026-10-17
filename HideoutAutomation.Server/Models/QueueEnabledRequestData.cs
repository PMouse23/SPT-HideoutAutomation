using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace HideoutAutomation.Server.Models;

public sealed class QueueEnabledRequestData : IRequestData
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
