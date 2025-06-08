using System;
using System.Threading.Tasks;
using Emma.Data;
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace TestRecordCreator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Emma AI Platform - Test Record Creator");
            Console.WriteLine("======================================");

            if (args.Length > 0 && args[0].ToLower() == "view")
            {
                // Only view records without adding new ones
                await ViewRecords.ViewAllRecords();
                return;
            }

            // Load connection string from environment variable
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Error: PostgreSQL connection string not found in environment variables.");
                Console.WriteLine("Please run load-env.ps1 first.");
                return;
            }

            Console.WriteLine("Creating DbContext with connection string...");
            
            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            try
            {
                // Create and save a new test entity
                using (var context = new AppDbContext(optionsBuilder.Options))
                {
                    Console.WriteLine("Adding test record to database...");
                    
                    var testEntity = new TestEntity
                    {
                        Name = "Test Record " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = "This record was created by the Emma AI Platform test script",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.TestEntities.Add(testEntity);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"Successfully added test record with ID: {testEntity.Id}");
                }
                
                Console.WriteLine("\nRecord added successfully. Viewing all records:");
                
                // Display all records after adding the new one
                await ViewRecords.ViewAllRecords();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
