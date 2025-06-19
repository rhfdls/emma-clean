using System;
using System.Threading.Tasks;
using System.Linq;
using Emma.Data;
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace TestRecordCreator
{
    public class ViewRecords
    {
        public static async Task ViewAllRecords()
        {
            // Load connection string from environment variable
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Error: PostgreSQL connection string not found in environment variables.");
                Console.WriteLine("Please run load-env.ps1 first.");
                return;
            }

            Console.WriteLine("Connecting to Emma AI Platform Azure PostgreSQL database...");
            
            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            try
            {
                // Query and display all test records
                using (var context = new AppDbContext(optionsBuilder.Options))
                {
                    var records = await context.TestEntities.OrderBy(e => e.Id).ToListAsync();
                    
                    Console.WriteLine("\nRecords in test_entities table:");
                    Console.WriteLine("=============================");
                    
                    if (records.Count == 0)
                    {
                        Console.WriteLine("No records found in the table.");
                    }
                    else
                    {
                        Console.WriteLine($"Found {records.Count} record(s):");
                        Console.WriteLine("");
                        
                        Console.WriteLine($"{"ID",-5} {"Name",-40} {"Description",-50} {"Created At",-25}");
                        Console.WriteLine(new string('-', 120));
                        
                        foreach (var record in records)
                        {
                            Console.WriteLine($"{record.Id,-5} {record.Name,-40} {record.Description,-50} {record.CreatedAt,-25}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing the database: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
