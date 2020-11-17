using System;
using TasmotaController.Services;

namespace TasmotaController
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Tasmota Controller starting now");

            new RateMonitor().Run();

            Console.WriteLine("Tasmota Controller finished");
        }
    }
}
