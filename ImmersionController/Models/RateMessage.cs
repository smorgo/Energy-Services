using System;

namespace ImmersionController.Models
{
    public class RateMessage
    {
        public DateTime Time {get; set;}
        public decimal Rate {get; set;}

        public static RateMessage TryParse(string message)
        {
            // RATE:2020-11-15T13:38Z,11.89

            if(message.Length > 19 && message.StartsWith("RATE:"))
            {
                var parts = message.Substring(5).Split(',');

                if(parts.Length >= 2) 
                {
                    DateTime time;

                    if(DateTime.TryParse(parts[0], out time))
                    {
                        decimal rate;

                        if(decimal.TryParse(parts[1], out rate))
                        {
                            return new RateMessage
                            {
                                Time = time,
                                Rate = rate
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}