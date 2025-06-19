using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Emma.Data;
using Emma.Models.Models;

namespace Emma.IntegrationTests.TestData
{
    public static class TestDataSeeder
    {
        public static async Task SeedTestData(AppDbContext context)
        {
            // Clear existing data
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // Add test organization
            var organization = new Organization
            {
                Id = 1,
                Name = "Test Organization",
                Subdomain = "testorg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.Organizations.AddAsync(organization);

            // Add test subscription
            var subscription = new Subscription
            {
                Id = 1,
                OrganizationId = organization.Id,
                Name = "Test Subscription",
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.Subscriptions.AddAsync(subscription);

            // Add test users with different roles
            var adminUser = new User
            {
                Id = 1,
                Email = "admin@testorg.com",
                FirstName = "Admin",
                LastName = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrganizationId = organization.Id
            };
            await context.Users.AddAsync(adminUser);

            // Add test contacts
            var owner = new Contact
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1234567890",
                RelationshipState = RelationshipState.Owner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrganizationId = organization.Id,
                CreatedById = adminUser.Id
            };

            var client = new Contact
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Phone = "+1987654321",
                RelationshipState = RelationshipState.Client,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrganizationId = organization.Id,
                CreatedById = adminUser.Id,
                RelationshipState = RelationshipState.Client,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SubscriptionId = subscription.Id,
                LastContactDate = DateTime.UtcNow.AddDays(-7)
            };

            context.Contacts.AddRange(owner, client);

            // Add test interactions
            var interaction1 = new Interaction
            {
                Id = 1,
                ContactId = client.Id,
                Type = InteractionType.Email,
                Content = "Initial consultation email",
                Timestamp = DateTime.UtcNow.AddDays(-7),
                Direction = InteractionDirection.Inbound,
                Status = InteractionStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var interaction2 = new Interaction
            {
                Id = 2,
                ContactId = client.Id,
                Type = InteractionType.Call,
                Content = "Follow-up call about property",
                Timestamp = DateTime.UtcNow.AddDays(-3),
                Direction = InteractionDirection.Outbound,
                Status = InteractionStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Interactions.AddRange(interaction1, interaction2);

            // Add test NBA recommendations
            var nba = new NbaRecommendation
            {
                Id = 1,
                ContactId = client.Id,
                Type = NbaRecommendationType.FollowUp,
                Priority = NbaPriority.High,
                Status = NbaStatus.Pending,
                Title = "Follow up with Jane Smith",
                Description = "Schedule a property viewing",
                DueDate = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.NbaRecommendations.Add(nba);

            // Save all changes
            context.SaveChanges();
        }
    }
}
