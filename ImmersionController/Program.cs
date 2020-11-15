using System;
using ImmersionController.Services;

namespace ImmersionController
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Immersion Controller starting now");

            new RateMonitor().Run();

            Console.WriteLine("Immersion Controller finished");
        }
    }
}
