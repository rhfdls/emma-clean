using Emma.Models.Interfaces;
using Emma.Models.Models;
using Emma.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Emma.Data;

public class AppDbContext : DbContext, IAppDbContext
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

    public DbSet<User> Users { get; set; }
    public DbSet<ResourceRecommendation> ResourceRecommendations { get; set; }
    public DbSet<UserPhoneNumber> UserPhoneNumbers { get; set; }
    public DbSet<UserSubscriptionAssignment> UserSubscriptionAssignments { get; set; }
    
    
    // Contact Management
    public DbSet<ContactCollaborator> ContactCollaborators { get; set; }
    public DbSet<ContactAssignment> ContactAssignments { get; set; }
    public DbSet<AccessAuditLog> AccessAuditLogs { get; set; }

    // Explicit linkage and AI-generated artifacts
    public DbSet<InteractionTranscription> InteractionTranscriptions { get; set; }
    public DbSet<DetailedNote> DetailedNotes { get; set; }
    public DbSet<SummaryOfSummary> SummaryOfSummaries { get; set; }
    
    // NBA Context Management System
    public DbSet<ContactSummary> ContactSummaries { get; set; }
    public DbSet<ContactState> ContactStates { get; set; }
    public DbSet<InteractionEmbedding> InteractionEmbeddings { get; set; }
    
    // Subscription Management
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; }
    public DbSet<Feature> Features { get; set; }
    
    // Task Management
    public DbSet<TaskItem> TaskItems { get; set; }
    

    // Privacy and Access Control System
    // TODO: Implement AccessAuditLog entity for privacy enforcement

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite primary key for SubscriptionPlanFeature
        modelBuilder.Entity<SubscriptionPlanFeature>()
            .HasKey(spf => new { spf.SubscriptionPlanId, spf.FeatureId });


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
        
        // Contact entity configuration
        modelBuilder.Entity<Contact>().HasKey(c => c.Id);
        modelBuilder.Entity<Contact>().HasIndex(c => c.OwnerId);
        
        // Configure TaskItem entity
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContactId);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.AssignedToUserId);

            entity.Property(e => e.Status).HasDefaultValue("pending");
            entity.Property(e => e.Priority).HasDefaultValue("normal");

            // Recurrence and dependencies
            entity.HasOne(e => e.ParentTask)
                .WithMany(e => e.DependentTasks)
                .HasForeignKey(e => e.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // NBA/AI integration fields are just string/double columns, no special mapping needed
            entity.Property(e => e.Source).HasMaxLength(32);
            entity.Property(e => e.SourceReferenceId).HasMaxLength(128);
            entity.Property(e => e.ApprovalStatus).HasMaxLength(32);

            // Metadata as JSONB
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
        });
        
        
        // EmailAddress entity configuration with unique constraint
        modelBuilder.Entity<EmailAddress>().HasKey(e => e.Id);
        modelBuilder.Entity<EmailAddress>()
            .HasIndex(e => e.Address)
            .IsUnique()
            .HasDatabaseName("IX_EmailAddresses_Address_Unique");
        
        // Contact -> EmailAddress relationship (one-to-many)
        modelBuilder.Entity<EmailAddress>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.EmailAddresses)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Contact>()
            .Property(c => c.PhoneNumbers)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Contact>()
            .Property(c => c.Tags)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Contact>()
            .Property(c => c.CustomFields)
            .HasColumnType("jsonb");
        
        modelBuilder.Entity<Contact>()
            .HasMany(c => c.Interactions)
            .WithOne(i => i.Contact)
            .HasForeignKey(i => i.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Message>().HasKey(m => m.Id);
        // Unique index for Message on OccurredAt and Type
        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.OccurredAt, m.Type })
            .IsUnique();
        // Message -> Interaction relationship
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Interaction)
            .WithMany(i => i.Messages)
            .HasForeignKey(m => m.InteractionId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Interaction -> Contact relationship
        modelBuilder.Entity<Interaction>()
            .HasOne(i => i.Contact)
            .WithMany(c => c.Interactions)
            .HasForeignKey(i => i.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Transcription>().HasKey(t => t.Id);
        modelBuilder.Entity<Organization>().HasKey(o => o.Id);
        modelBuilder.Entity<Organization>().HasIndex(o => o.Email).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.FubApiKey).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.FubSystem).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.FubSystemKey).IsUnique();
        modelBuilder.Entity<Agent>().HasKey(a => a.Id);
        modelBuilder.Entity<PasswordResetToken>().HasKey(prt => prt.Id);
        modelBuilder.Entity<ConversationSummary>().HasKey(cs => cs.Id);
        modelBuilder.Entity<ConversationSummary>().HasIndex(cs => cs.InteractionId).IsUnique();
        modelBuilder.Entity<ConversationSummary>().HasIndex(cs => cs.QualityScore);
        modelBuilder.Entity<Subscription>().HasKey(s => s.UserId);
        modelBuilder.Entity<AgentPhoneNumber>()
            .HasKey(apn => apn.Id);
        modelBuilder.Entity<AgentPhoneNumber>()
            .HasIndex(apn => apn.PhoneNumber);
        modelBuilder.Entity<AgentPhoneNumber>()
            .HasIndex(apn => apn.AgentId);
        modelBuilder.Entity<DeviceToken>()
            .HasKey(d => new { d.UserId, d.DeviceId });
        modelBuilder.Entity<CallMetadata>().HasKey(m => m.MessageId);
        
        modelBuilder.Entity<ContactSummary>()
            .Property(cs => cs.KeyMilestones)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactSummary>()
            .Property(cs => cs.ImportantPreferences)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, object>() : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");
        modelBuilder.Entity<ContactSummary>()
            .Property(cs => cs.CustomFields)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, object>() : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
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
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .Property(ie => ie.ExtractedEntities)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, object>() : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .Property(ie => ie.Topics)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .Property(ie => ie.CustomFields)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, object>() : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");
        modelBuilder.Entity<InteractionEmbedding>()
            .HasIndex(ie => new { ie.ContactId, ie.OrganizationId, ie.Timestamp });
        modelBuilder.Entity<InteractionEmbedding>()
            .HasIndex(ie => ie.InteractionId)
            .IsUnique();
        
        modelBuilder.Entity<AccessAuditLog>().HasKey(aal => aal.Id);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.RequestingUserId);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.ContactId);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.AccessedAt);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.ResourceType);
        modelBuilder.Entity<AccessAuditLog>().HasIndex(aal => aal.OrganizationId);
        
        modelBuilder.Entity<AccessAuditLog>()
            .HasOne(aal => aal.RequestingUser)
            .WithMany()
            .HasForeignKey(aal => aal.RequestingUserId)
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
            .WithOne()
            .HasForeignKey(cr => cr.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
        

    }
}

