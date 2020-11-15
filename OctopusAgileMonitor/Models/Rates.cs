using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OctopusAgileMonitor.Models
{
    public class Rates
    {
        [JsonPropertyName("count")]
        public int Count {get; set;}

        [JsonPropertyName("results")]
        public List<Rate> Results {get; set;}
    }
}