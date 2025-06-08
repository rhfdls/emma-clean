// Windsurf Prompt: Diagnose Env Var Case Mismatches for Azure & Cosmos DB

using System;
using System.Collections.Generic;

namespace Emma.EnvCaseValidator
{
    public static class EnvCaseValidator
    {
        public static void Run()
        {
            var expectedVars = new List<string>
            {
                // List each logical key once (will check all common forms)
                "AZUREAIFOUNDRY__ENDPOINT",
                "AZUREAIFOUNDRY__APIKEY",
                "AZUREAIFOUNDRY__DEPLOYMENTNAME",
                "AZUREOPENAI__ENDPOINT",
                "AZUREOPENAI__APIKEY",
                "AZUREOPENAI__DEPLOYMENTNAME",
                "COSMOSDB__ACCOUNTENDPOINT",
                "COSMOSDB__ACCOUNTKEY",
                "COSMOSDB__DATABASENAME",
                "COSMOSDB__CONTAINERNAME"
            };

            Console.WriteLine("=== EMMA Environment Variable Case Diagnostic ===");

            foreach (var baseKey in expectedVars)
            {
                // Check UPPERCASE
                var upper = Environment.GetEnvironmentVariable(baseKey);
                // Check PascalCase (first letter upper, rest as in example)
                var pascal = Environment.GetEnvironmentVariable(ToPascal(baseKey));
                // Check lowercase (rare but possible)
                var lower = Environment.GetEnvironmentVariable(baseKey.ToLower());

                Console.WriteLine($"\nVariable: {baseKey}");
                Console.WriteLine($"  UPPERCASE      : {(upper == null ? "[MISSING]" : "[SET]")}");
                Console.WriteLine($"  PascalCase     : {(pascal == null ? "[MISSING]" : "[SET]")}  [{ToPascal(baseKey)}]");
                Console.WriteLine($"  lowercase      : {(lower == null ? "[MISSING]" : "[SET]")}");
            }
            Console.WriteLine("\nIf only one case is [SET], ensure your .env, Compose, and code/validator ALL use that exact case!");
        }

        static string ToPascal(string input)
        {
            // Converts COSMOSDB__ACCOUNTKEY -> CosmosDb__AccountKey
            var parts = input.ToLower().Split("__");
            for (int i = 0; i < parts.Length; i++)
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join("__", parts);
        }
    }
}
