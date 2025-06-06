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

    // NBA Context Management System
    public DbSet<ContactSummary> ContactSummaries { get; set; }
    public DbSet<ContactState> ContactStates { get; set; }
    public DbSet<InteractionEmbedding> InteractionEmbeddings { get; set; }

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
        modelBuilder.Entity<Contact>().HasKey(c => c.Id);
        modelBuilder.Entity<Contact>().HasIndex(c => c.OwnerId);
        
        // EmailAddress entity configuration with unique constraint
        modelBuilder.Entity<EmailAddress>().HasKey(e => e.Id);
        modelBuilder.Entity<EmailAddress>()
            .HasIndex(e => e.Address)
            .IsUnique()
            .HasDatabaseName("IX_EmailAddresses_Address_Unique");
        
        // Contact -> EmailAddress relationship (one-to-many)
        modelBuilder.Entity<EmailAddress>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Emails)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Contact>()
            .Property(c => c.Phones)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Contact>()
            .Property(c => c.Tags)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Contact>()
            .Property(c => c.CustomFields)
            .HasColumnType("jsonb");
        
        // Contact -> Agent (Owner) relationship
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Message>().HasKey(m => m.Id);
        // Unique index for Message on OccurredAt and Type
        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.OccurredAt, m.Type })
            .IsUnique();
        // Agent-Message one-to-many
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Agent)
            .WithMany(a => a.Messages)
            .HasForeignKey(m => m.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Interaction -> Contact relationship
        modelBuilder.Entity<Interaction>()
            .HasOne(i => i.Contact)
            .WithMany(c => c.Interactions)
            .HasForeignKey(i => i.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Interaction -> Agent relationship
        modelBuilder.Entity<Interaction>()
            .HasOne(i => i.Agent)
            .WithMany()
            .HasForeignKey(i => i.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Transcription>().HasKey(t => t.Id);
        modelBuilder.Entity<Organization>().HasKey(o => o.Id);
        modelBuilder.Entity<Organization>().HasIndex(o => o.Email).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.FubApiKey).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.FubSystem).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.FubSystemKey).IsUnique();
        modelBuilder.Entity<Agent>().HasKey(a => a.Id);
        modelBuilder.Entity<Agent>().HasIndex(a => a.Email).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasKey(prt => prt.Id);
        modelBuilder.Entity<ConversationSummary>().HasKey(cs => cs.Id);
        modelBuilder.Entity<ConversationSummary>().HasIndex(cs => cs.ConversationId).IsUnique();
        modelBuilder.Entity<ConversationSummary>().HasIndex(cs => cs.QualityScore);
        modelBuilder.Entity<Subscription>().HasKey(s => s.AgentId);
        modelBuilder.Entity<AgentPhoneNumber>().HasKey(p => p.Id);
        modelBuilder.Entity<AgentPhoneNumber>().HasIndex(p => p.Number).IsUnique();
        modelBuilder.Entity<DeviceToken>().HasKey(d => new { d.AgentId, d.DeviceId });
        modelBuilder.Entity<CallMetadata>().HasKey(m => m.MessageId);
        
        // Resource Assignment System Configurations
        
        // ResourceCategory
        modelBuilder.Entity<ResourceCategory>().HasKey(rc => rc.Id);
        modelBuilder.Entity<ResourceCategory>().HasIndex(rc => rc.Name).IsUnique();
        modelBuilder.Entity<ResourceCategory>().HasIndex(rc => rc.SortOrder);
        
        // Resource
        modelBuilder.Entity<Resource>().HasKey(r => r.Id);
        modelBuilder.Entity<Resource>().HasIndex(r => new { r.OrganizationId, r.Name, r.CategoryId });
        modelBuilder.Entity<Resource>().HasIndex(r => new { r.CategoryId, r.IsPreferred, r.Rating });
        
        // Resource -> ResourceCategory
        modelBuilder.Entity<Resource>()
            .HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Resource -> Organization
        modelBuilder.Entity<Resource>()
            .HasOne(r => r.Organization)
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Resource -> Agent (CreatedBy)
        modelBuilder.Entity<Resource>()
            .HasOne(r => r.CreatedByAgent)
            .WithMany()
            .HasForeignKey(r => r.CreatedByAgentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Resource -> Agent (for collaborator resources)
        modelBuilder.Entity<Resource>()
            .HasOne(r => r.Agent)
            .WithMany()
            .HasForeignKey(r => r.AgentId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // ResourceAssignment
        modelBuilder.Entity<ResourceAssignment>().HasKey(ra => ra.Id);
        modelBuilder.Entity<ResourceAssignment>().HasIndex(ra => new { ra.ContactId, ra.Status });
        modelBuilder.Entity<ResourceAssignment>().HasIndex(ra => new { ra.OrganizationId, ra.AssignedAt });
        modelBuilder.Entity<ResourceAssignment>().HasIndex(ra => new { ra.ResourceId, ra.Status });
        
        // ResourceAssignment -> Contact
        modelBuilder.Entity<ResourceAssignment>()
            .HasOne(ra => ra.Contact)
            .WithMany()
            .HasForeignKey(ra => ra.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceAssignment -> Resource
        modelBuilder.Entity<ResourceAssignment>()
            .HasOne(ra => ra.Resource)
            .WithMany()
            .HasForeignKey(ra => ra.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceAssignment -> Agent (AssignedBy)
        modelBuilder.Entity<ResourceAssignment>()
            .HasOne(ra => ra.AssignedByAgent)
            .WithMany()
            .HasForeignKey(ra => ra.AssignedByAgentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceAssignment -> Organization
        modelBuilder.Entity<ResourceAssignment>()
            .HasOne(ra => ra.Organization)
            .WithMany()
            .HasForeignKey(ra => ra.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceAssignment -> Interaction
        modelBuilder.Entity<ResourceAssignment>()
            .HasOne(ra => ra.Interaction)
            .WithMany()
            .HasForeignKey(ra => ra.InteractionId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // ResourceRecommendation
        modelBuilder.Entity<ResourceRecommendation>().HasKey(rr => rr.Id);
        modelBuilder.Entity<ResourceRecommendation>().HasIndex(rr => new { rr.ContactId, rr.RecommendedAt });
        modelBuilder.Entity<ResourceRecommendation>().HasIndex(rr => new { rr.ResourceId, rr.WasSelected });
        modelBuilder.Entity<ResourceRecommendation>().HasIndex(rr => new { rr.OrganizationId, rr.RecommendedAt });
        
        // ResourceRecommendation -> Contact
        modelBuilder.Entity<ResourceRecommendation>()
            .HasOne(rr => rr.Contact)
            .WithMany()
            .HasForeignKey(rr => rr.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceRecommendation -> Resource
        modelBuilder.Entity<ResourceRecommendation>()
            .HasOne(rr => rr.Resource)
            .WithMany()
            .HasForeignKey(rr => rr.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceRecommendation -> Agent (RecommendedBy)
        modelBuilder.Entity<ResourceRecommendation>()
            .HasOne(rr => rr.RecommendedByAgent)
            .WithMany()
            .HasForeignKey(rr => rr.RecommendedByAgentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceRecommendation -> Organization
        modelBuilder.Entity<ResourceRecommendation>()
            .HasOne(rr => rr.Organization)
            .WithMany()
            .HasForeignKey(rr => rr.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // ResourceRecommendation -> Interaction
        modelBuilder.Entity<ResourceRecommendation>()
            .HasOne(rr => rr.Interaction)
            .WithMany()
            .HasForeignKey(rr => rr.InteractionId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // Configure Dictionary properties to use JSON instead of hstore for Azure PostgreSQL compatibility
        
        // Conversation
        modelBuilder.Entity<Conversation>()
            .Property(c => c.ExternalIds)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Conversation>()
            .Property(c => c.CustomFields)
            .HasColumnType("jsonb");
            
        // Interaction
        modelBuilder.Entity<Interaction>()
            .Property(i => i.ExternalIds)
            .HasColumnType("jsonb");
        modelBuilder.Entity<Interaction>()
            .Property(i => i.CustomFields)
            .HasColumnType("jsonb");
            
        // Resource
        modelBuilder.Entity<Resource>()
            .Property(r => r.CustomFields)
            .HasColumnType("jsonb");
            
        // ResourceAssignment
        modelBuilder.Entity<ResourceAssignment>()
            .Property(ra => ra.CustomFields)
            .HasColumnType("jsonb");
            
        // ResourceRecommendation
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
    }
}
