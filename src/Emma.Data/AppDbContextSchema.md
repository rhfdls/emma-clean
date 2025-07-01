# EMMA Database Schema Documentation

This document contains the full content of the `AppDbContext.cs` file, which defines the database schema for the EMMA platform. It includes entity definitions and relationship configurations.

```csharp
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Interaction> Interactions { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Transcription> Transcriptions { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<EmailAddress> EmailAddresses { get; set; }
    public DbSet<EmmaAnalysis> EmmaAnalyses { get; set; }
    public DbSet<AgentAssignment> AgentAssignments { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<ConversationSummary> ConversationSummaries { get; set; }
    public DbSet<CallMetadata> CallMetadata { get; set; }
    public DbSet<AgentPhoneNumber> AgentPhoneNumbers { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<TestEntity> TestEntities { get; set; }
    
    // Resource Assignment System (OBSOLETE - migrating to Contact-centric)
    public DbSet<ResourceCategory> ResourceCategories { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceAssignment> ResourceAssignments { get; set; }
    public DbSet<ResourceRecommendation> ResourceRecommendations { get; set; }

    // Contact Assignment System (NEW - Contact-centric approach)
    public DbSet<ContactAssignment> ContactAssignments { get; set; }
    public DbSet<ContactCollaborator> ContactCollaborators { get; set; }
    public DbSet<AccessAuditLog> AccessAuditLogs { get; set; }

    // NBA Context Management System
    public DbSet<ContactSummary> ContactSummaries { get; set; }
    public DbSet<ContactState> ContactStates { get; set; }
    public DbSet<InteractionEmbedding> InteractionEmbeddings { get; set; }

    // Privacy and Access Control System
    // TODO: Implement AccessAuditLog entity for privacy enforcement

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite primary key for SubscriptionPlanFeature
        modelBuilder.Entity<SubscriptionPlanFeature>()
            .HasKey(spf => new { spf.SubscriptionPlanId, spf.FeatureId });

        // Subscription-Agent (one-to-one)
        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Agent)
            .WithOne(a => a.Subscription)
            .HasForeignKey<Subscription>(s => s.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Subscription-Plan (many-to-one)
        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Explicit Agent <-> Organization relationship
        modelBuilder.Entity<Agent>()
            .HasOne(a => a.Organization)
            .WithMany(o => o.Agents)
            .HasForeignKey(a => a.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Explicit Organization <-> OwnerAgent relationship
        modelBuilder.Entity<Organization>()
            .HasOne(o => o.OwnerAgent)
            .WithMany()
            .HasForeignKey(o => o.OwnerAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        // AgentAssignment is a true entity with a foreign key to EmmaAnalysis
        modelBuilder.Entity<AgentAssignment>()
            .HasKey(a => a.Id);
        modelBuilder.Entity<AgentAssignment>()
            .HasOne(a => a.EmmaAnalysis)
            .WithMany(e => e.AgentAssignments)
            .HasForeignKey(a => a.EmmaAnalysisId)
            .IsRequired();
        modelBuilder.Entity<Interaction>().HasKey(c => c.Id);
        modelBuilder.Entity<Interaction>().HasIndex(c => c.ContactId).IsUnique();
        modelBuilder.Entity<Interaction>().HasIndex(c => c.AgentId);
        
        // Contact entity configuration
        modelBuilder.Entity<ResourceRecommendation>()
            .Property(rr => rr.CustomFields)
            .HasColumnType("jsonb");
            
        // NBA Context Management Models
        
        // ContactSummary
        modelBuilder.Entity<ContactSummary>()
            .Property(cs => cs.KeyMilestones)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactSummary>()
            .Property(cs => cs.ImportantPreferences)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactSummary>()
            .Property(cs => cs.CustomFields)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactSummary>()
            .HasIndex(cs => new { cs.ContactId, cs.OrganizationId, cs.SummaryType });
            
        // ContactState
        modelBuilder.Entity<ContactState>()
            .Property(cs => cs.PendingTasks)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactState>()
            .Property(cs => cs.OpenObjections)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactState>()
            .Property(cs => cs.ImportantDates)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactState>()
            .Property(cs => cs.PropertyInfo)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactState>()
            .Property(cs => cs.FinancialInfo)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactState>()
            .Property(cs => cs.CustomFields)
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactState>()
            .HasIndex(cs => new { cs.ContactId, cs.OrganizationId })
            .IsUnique();
            
        // InteractionEmbedding
        modelBuilder.Entity<InteractionEmbedding>()
            .Property(ie => ie.PrivacyTags)
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .Property(ie => ie.ExtractedEntities)
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .Property(ie => ie.Topics)
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .Property(ie => ie.CustomFields)
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .HasIndex(ie => new { ie.ContactId, ie.OrganizationId, ie.Timestamp });
        modelBuilder.Entity<InteractionEmbedding>()
            .HasIndex(ie => ie.InteractionId)
            .IsUnique();
        
        modelBuilder.Entity<AccessAuditLog>().HasKey(aal => aal.Id);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.RequestingAgentId);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.ContactId);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.AccessedAt);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.ResourceType);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.OrganizationId);
        
        modelBuilder.Entity<AccessAuditLog>()
            .HasOne(aal => aal.RequestingAgent)
            .WithMany()
            .HasForeignKey(aal => aal.RequestingAgentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<AccessAuditLog>()
            .HasOne(aal => aal.Contact)
            .WithMany()
            .HasForeignKey(aal => aal.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<AccessAuditLog>()
            .HasOne(aal => aal.Organization)
            .WithMany()
            .HasForeignKey(aal => aal.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<AccessAuditLog>()
            .Property(aal => aal.PrivacyTags)
            .HasColumnType("jsonb");
        
        modelBuilder.Entity<Contact>()
            .HasMany(c => c.AssignedResources)
            .WithOne(ca => ca.ClientContact)
            .HasForeignKey(ca => ca.ClientContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

```
