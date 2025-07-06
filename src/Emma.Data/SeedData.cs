using System;
using System.Linq;
using Emma.Models.Models;

namespace Emma.Data
{
    public static class SeedData
    {
        public static void EnsureSeeded(AppDbContext context)
        {
            // Seed Organization
            var orgName = "Region Realty";
            var org = context.Organizations.FirstOrDefault(o => o.Name == orgName);
            if (org == null)
            {
                org = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = orgName,
                    CreatedAt = DateTime.UtcNow
                };
                context.Organizations.Add(org);
                context.SaveChanges();
            }

            // Seed Agent (AI agent, not a user)
            var agentName = "EmmaBot";
            var agent = context.Agents.FirstOrDefault(a => a.Name == agentName && a.OrganizationId == org.Id);
            if (agent == null)
            {
                agent = new Agent
                {
                    Id = Guid.NewGuid(),
                    Name = agentName,
                    AgentType = "EmmaBot",
                    OrganizationId = org.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Agents.Add(agent);
                context.SaveChanges();
            }

            // Seed Contact
            var contactFirstName = "TestClient";
            var contactLastName = "Example";
            var contact = context.Contacts.FirstOrDefault(c => c.FirstName == contactFirstName && c.LastName == contactLastName && c.OrganizationId == org.Id);
            if (contact == null)
            {
                contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = contactFirstName,
                    LastName = contactLastName,
                    OrganizationId = org.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Contacts.Add(contact);
                context.SaveChanges();
            }

            // Seed Interaction for the Contact and Organization
            var interaction = context.Interactions.FirstOrDefault(i => i.ContactId == contact.Id && i.OrganizationId == org.Id);
            if (interaction == null)
            {
                interaction = new Interaction
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = org.Id,
                    ContactId = contact.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Interactions.Add(interaction);
                context.SaveChanges();
            }

            // Seed Message for the Contact (using UserId if required by schema)
            var messageOccurredAt = DateTime.UtcNow.Date.AddHours(9); // 9am today
            var messageType = Emma.Models.Enums.MessageType.Text;
            var existingMessage = context.Messages.FirstOrDefault(m => m.InteractionId == interaction.Id && m.OccurredAt == messageOccurredAt && m.Type == messageType);
            if (existingMessage == null)
            {
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    Payload = "Welcome to Emma!",
                    BlobStorageUrl = "http://example.com/blob/1",
                    Type = messageType,
                    CreatedAt = DateTime.UtcNow,
                    OccurredAt = messageOccurredAt,
                    InteractionId = interaction.Id,
                    // UserId = ... // Set if required by your Message schema
                };
                context.Messages.Add(message);
                context.SaveChanges();
            }
        }
    }
}
