using System;
using OctopusAgileMonitor.Services;

namespace OctopusAgileMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("kube-fan-controller starting");

            new AgileTariffMonitor().Run();

            Console.WriteLine("kube-fan-controller finished");
        }
    }
}
