using System;
using System.Linq;
using Emma.Data.Models;

namespace Emma.Data
{
    public static class SeedData
    {
        public static void EnsureSeeded(AppDbContext context)
        {
            // Seed Organization
            var orgEmail = "regionrealty@example.com";
            var org = context.Organizations.FirstOrDefault(o => o.Email == orgEmail);
            if (org == null)
            {
                org = new Organization
                {
                    Id = Guid.NewGuid(),
                    Email = orgEmail,
                    FubApiKey = string.Empty,
                    FubSystem = string.Empty,
                    FubSystemKey = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    Agents = new System.Collections.Generic.List<Agent>()
                };
                context.Organizations.Add(org);
                context.SaveChanges();
            }

            // Seed Agent
            var agentEmail = "able.tester@regionrealty.com";
            var agent = context.Agents.FirstOrDefault(a => a.Email == agentEmail);
            if (agent == null)
            {
                agent = new Agent
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Able",
                    LastName = "Tester",
                    Email = agentEmail,
                    OrganizationId = org.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Agents.Add(agent);
                context.SaveChanges();
            }
            // Seed Interaction for the Agent and Organization
            var conversation = context.Interactions.FirstOrDefault(c => c.AgentId == agent.Id && c.OrganizationId == org.Id);
            if (conversation == null)
            {
                conversation = new Interaction
                {
                    Id = Guid.NewGuid(),
                    AgentId = agent.Id,
                    OrganizationId = org.Id,
                    ClientFirstName = "TestClient",
                    ClientLastName = "Example",
                    CreatedAt = DateTime.UtcNow
                };
                context.Interactions.Add(conversation);
                context.SaveChanges();
            }
            // Seed Message for the Agent
            var messageOccurredAt = DateTime.UtcNow.Date.AddHours(9); // 9am today
            var messageType = Emma.Data.Enums.MessageType.Text;
            var existingMessage = context.Messages.FirstOrDefault(m => m.AgentId == agent.Id && m.OccurredAt == messageOccurredAt && m.Type == messageType);
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
                    AgentId = agent.Id,
                    InteractionId = conversation.Id,
                };
                context.Messages.Add(message);
                context.SaveChanges();
            }
        }
    }
}
