using System;
using OctopusAgileMonitor.Extensions;

namespace OctopusAgileMonitor.Models
{
    public class CurrentRate
    {
        public DateTime Now {get; set;}
        public decimal Rate {get;set;}
        public string Payload
        {
            get
            {
                return $"RATE:{Now.ToIso8601()},{Rate:F2}";
            }
        }
    }
}