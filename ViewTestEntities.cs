using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Emma.Data;
using Emma.Data.Models;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Emma AI Platform - Test Entities Viewer");
        Console.WriteLine("=======================================");

        // Get connection string from environment variable
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: PostgreSQL connection string not found");
            Console.WriteLine("Make sure to run load-env.ps1 first");
            return;
        }

        try
        {
            // Create options and context
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Build();

            using (var context = new AppDbContext(options))
            {
                Console.WriteLine("Connected to Azure PostgreSQL database");
                
                // Count records
                var count = await context.TestEntities.CountAsync();
                Console.WriteLine($"Total records in test_entities table: {count}");
                
                if (count > 0)
                {
                    // Get all records
                    var records = await context.TestEntities
                        .OrderBy(r => r.Id)
                        .ToListAsync();
                    
                    Console.WriteLine("\nID | Name | Description | Created At");
                    Console.WriteLine("---------------------------------------------------");
                    
                    foreach (var record in records)
                    {
                        Console.WriteLine($"{record.Id} | {record.Name} | {record.Description} | {record.CreatedAt}");
                    }
                }
                else
                {
                    Console.WriteLine("No records found in the test_entities table");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
