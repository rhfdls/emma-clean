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
        Console.WriteLine("Loading connection string from environment...");

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: PostgreSQL connection string not found");
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Build();

            using (var context = new AppDbContext(options))
            {
                Console.WriteLine("Connected to Azure PostgreSQL database");
                
                var count = await context.TestEntities.CountAsync();
                Console.WriteLine($"Total records in test_entities table: {count}");
                
                if (count > 0)
                {
                    var records = await context.TestEntities
                        .OrderBy(r => r.Id)
                        .ToListAsync();
                    
                    Console.WriteLine("\nRecords in test_entities table:");
                    Console.WriteLine("-----------------------------");
                    
                    foreach (var record in records)
                    {
                        Console.WriteLine($"ID: {record.Id}");
                        Console.WriteLine($"Name: {record.Name}");
                        Console.WriteLine($"Description: {record.Description}");
                        Console.WriteLine($"Created At: {record.CreatedAt}");
                        Console.WriteLine("-----------------------------");
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
