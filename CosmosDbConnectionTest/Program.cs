using System;
using System.Threading.Tasks;

namespace CosmosDbConnectionTest
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Emma AI Platform - CosmosDB Connection Test");
            Console.WriteLine("Running simple CosmosDB connectivity test...");
            Console.WriteLine();

            await SimpleTest.RunTest();

            Console.WriteLine();
            Console.WriteLine("Test completed. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
