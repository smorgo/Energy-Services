using System;
using System.Text.Json.Serialization;

namespace OctopusAgileMonitor.Models
{
    public class Rate
    {
        [JsonPropertyName("value_exc_vat")]
        public decimal ValueExcVAT {get; set;}
        [JsonPropertyName("value_inc_vat")]
        public decimal ValueIncVAT {get; set;}
        [JsonPropertyName("valid_from")]
        public DateTime ValidFrom {get; set;}
        [JsonPropertyName("valid_to")]
        public DateTime ValidTo {get; set;}
    }
}