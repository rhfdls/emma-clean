using Microsoft.EntityFrameworkCore;
using Emma.Models.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Emma.Models.Interfaces
{
    public interface IAppDbContext
    {
        // Core entities
        DbSet<Contact> Contacts { get; set; }
        DbSet<Interaction> Interactions { get; set; }
        DbSet<Message> Messages { get; set; }
        DbSet<TaskItem> TaskItems { get; set; }
        DbSet<Transcription> Transcriptions { get; set; }
        DbSet<Organization> Organizations { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<Agent> Agents { get; set; }
        DbSet<EmailAddress> EmailAddresses { get; set; }
        DbSet<EmmaAnalysis> EmmaAnalyses { get; set; }
        DbSet<DeviceToken> DeviceTokens { get; set; }
        DbSet<Subscription> Subscriptions { get; set; }
        DbSet<TestEntity> TestEntities { get; set; }
        
        // User management
        DbSet<UserPhoneNumber> UserPhoneNumbers { get; set; }
        DbSet<UserSubscriptionAssignment> UserSubscriptionAssignments { get; set; }
        DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        
        // Contact management
        DbSet<ContactCollaborator> ContactCollaborators { get; set; }
        DbSet<ContactAssignment> ContactAssignments { get; set; }
        DbSet<AccessAuditLog> AccessAuditLogs { get; set; }
        
        // NBA Context Management System
        DbSet<ContactSummary> ContactSummaries { get; set; }
        DbSet<ContactState> ContactStates { get; set; }
        DbSet<InteractionEmbedding> InteractionEmbeddings { get; set; }
        
        // Subscription management
        DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; }
        DbSet<Feature> Features { get; set; }
        
        // Resource management (legacy, being migrated)
            DbSet<ResourceRecommendation> ResourceRecommendations { get; set; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
