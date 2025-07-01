using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emma.Models.Enums;
using Emma.Models.Interfaces;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Emma.Infrastructure.Data
{
    /// <summary>
    /// The main database context for the EMMA application.
    /// </summary>
    public class EmmaDbContext : DbContext, IAppDbContext
    {
        public EmmaDbContext(DbContextOptions<EmmaDbContext> options) 
            : base(options)
        {
        }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous save operation. The task result contains the
        /// number of state entries written to the database.
        /// </returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified));

            var now = DateTime.UtcNow;

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    ((BaseEntity)entityEntry.Entity).CreatedAt = now;
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    ((BaseEntity)entityEntry.Entity).UpdatedAt = now;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        #region IAppDbContext Implementation
        
        // Core entities from IAppDbContext
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<Interaction> Interactions { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<TaskItem> TaskItems { get; set; } = null!;
        public DbSet<Transcription> Transcriptions { get; set; } = null!;
        public DbSet<Organization> Organizations { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Agent> Agents { get; set; } = null!;
        public DbSet<EmailAddress> EmailAddresses { get; set; } = null!;
        public DbSet<EmmaAnalysis> EmmaAnalyses { get; set; } = null!;
        public DbSet<DeviceToken> DeviceTokens { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        
        // User management
        public DbSet<UserPhoneNumber> UserPhoneNumbers { get; set; } = null!;
        public DbSet<UserSubscriptionAssignment> UserSubscriptionAssignments { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        
        // Contact management
        public DbSet<ContactCollaborator> ContactCollaborators { get; set; } = null!;
        public DbSet<ContactAssignment> ContactAssignments { get; set; } = null!;
        public DbSet<AccessAuditLog> AccessAuditLogs { get; set; } = null!;
        
        // NBA Context Management System
        public DbSet<ContactSummary> ContactSummaries { get; set; } = null!;
        public DbSet<ContactState> ContactStates { get; set; } = null!;
        public DbSet<InteractionEmbedding> InteractionEmbeddings { get; set; } = null!;
        
        // Subscription management
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; } = null!;
        public DbSet<Feature> Features { get; set; } = null!;
        
        // Resource management (legacy, being migrated)
        public DbSet<ResourceCategory> ResourceCategories { get; set; } = null!;
        public DbSet<Resource> Resources { get; set; } = null!;
        public DbSet<ResourceAssignment> ResourceAssignments { get; set; } = null!;
        public DbSet<ResourceRecommendation> ResourceRecommendations { get; set; } = null!;
        
        // Additional DbSets not in IAppDbContext but used internally
        public DbSet<ContactStateHistory> ContactStateHistories { get; set; } = null!;
        
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Apply configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmmaDbContext).Assembly);
            
            #region Entity Configurations
            
            // EmailAddress entity configuration
            modelBuilder.Entity<EmailAddress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.Verified).HasDefaultValue(false);
                entity.HasIndex(e => e.Address).IsUnique();
                
                // Relationship with User
                entity.HasOne<Models.User>()
                    .WithMany(u => u.EmailAddresses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Relationship with Contact
                entity.HasOne<Contact>()
                    .WithMany(c => c.EmailAddresses)
                    .HasForeignKey(e => e.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // TaskItem entity configuration
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(4000);
                entity.Property(t => t.Status).HasDefaultValue(TaskStatus.NotStarted);
                entity.Property(t => t.Priority).HasDefaultValue(TaskPriority.Medium);
                
                // Relationship with User (AssignedTo)
                entity.HasOne<Models.User>()
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(t => t.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Relationship with Contact
                entity.HasOne<Contact>()
                    .WithMany(c => c.Tasks)
                    .HasForeignKey(t => t.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Relationship with Interaction
                entity.HasOne<Interaction>()
                    .WithMany(i => i.Tasks)
                    .HasForeignKey(t => t.InteractionId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                // Indexes
                entity.HasIndex(t => t.DueDate);
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.Priority);
            });
            
            // AccessAuditLog entity configuration
            modelBuilder.Entity<AccessAuditLog>(entity =>
            {
                entity.ToTable("AccessAuditLogs");
                entity.HasKey(a => a.Id);
                
                entity.Property(a => a.Action).IsRequired().HasMaxLength(50);
                entity.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
                entity.Property(a => a.IpAddress).HasMaxLength(45);
                entity.Property(a => a.UserAgent).HasMaxLength(500);
                entity.Property(a => a.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Relationship with User
                entity.HasOne<Models.User>()
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                // Indexes
                entity.HasIndex(a => a.Timestamp);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            
            // Contact entity configuration
            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasKey(c => c.Id);
                
                // Property configurations
                entity.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.LastName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.MiddleName).HasMaxLength(100);
                entity.Property(c => c.PreferredName).HasMaxLength(100);
                entity.Property(c => c.Title).HasMaxLength(50);
                entity.Property(c => c.JobTitle).HasMaxLength(200);
                entity.Property(c => c.Company).HasMaxLength(200);
                entity.Property(c => c.Department).HasMaxLength(200);
                entity.Property(c => c.Source).HasMaxLength(100);
                entity.Property(c => c.PreferredContactMethod).HasMaxLength(50);
                entity.Property(c => c.PreferredContactTime).HasMaxLength(50);
                entity.Property(c => c.ProfilePictureUrl).HasMaxLength(500);
                
                // Configure enums
                entity.Property(c => c.RelationshipState)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                
                // Configure owned types
                entity.OwnsOne(c => c.Address, a =>
                {
                    a.Property(ad => ad.Street1).HasMaxLength(200);
                    a.Property(ad => ad.Street2).HasMaxLength(200);
                    a.Property(ad => ad.City).HasMaxLength(100);
                    a.Property(ad => ad.State).HasMaxLength(100);
                    a.Property(ad => ad.PostalCode).HasMaxLength(20);
                    a.Property(ad => ad.Country).HasMaxLength(2);
                    a.Property(ad => ad.Type).HasMaxLength(50);
                    a.Property(ad => ad.Notes).HasMaxLength(500);
                });
                
                // Relationships
                entity.HasOne(c => c.Organization)
                    .WithMany()
                    .HasForeignKey(c => c.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(c => c.Owner)
                    .WithMany()
                    .HasForeignKey(c => c.OwnerId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Indexes
                entity.HasIndex(c => c.OrganizationId);
                entity.HasIndex(c => c.OwnerId);
                entity.HasIndex(c => new { c.LastName, c.FirstName });
                entity.HasIndex(c => c.Company);
                entity.HasIndex(c => c.RelationshipState);
                entity.HasIndex(c => c.LastContactedAt);
                entity.HasIndex(c => c.NextFollowUpAt);
            });
            
            // PhoneNumber entity configuration
            modelBuilder.Entity<PhoneNumber>(entity =>
            {
                entity.HasKey(p => p.Id);
                
                // Property configurations
                entity.Property(p => p.Number).IsRequired().HasMaxLength(20);
                entity.Property(p => p.Type).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Notes).HasMaxLength(500);
                
                // Relationships
                entity.HasOne(p => p.Contact)
                    .WithMany(c => c.PhoneNumbers)
                    .HasForeignKey(p => p.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(p => p.User)
                    .WithMany(u => u.PhoneNumbers)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Indexes
                entity.HasIndex(p => p.Number);
                entity.HasIndex(p => p.Type);
                entity.HasIndex(p => p.IsPrimary);
                entity.HasIndex(p => p.ContactId);
                entity.HasIndex(p => p.UserId);
            });
            
            // ContactStateHistory entity configuration
            modelBuilder.Entity<ContactStateHistory>(entity =>
            {
                entity.HasKey(h => h.Id);
                
                // Property configurations
                entity.Property(h => h.Reason).HasMaxLength(1000);
                
                // Relationships
                entity.HasOne(h => h.Contact)
                    .WithMany(c => c.StateHistory)
                    .HasForeignKey(h => h.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(h => h.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(h => h.ChangedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Configure enums
                entity.Property(h => h.FromState)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                    
                entity.Property(h => h.ToState)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                
                // Indexes
                entity.HasIndex(h => h.ContactId);
                entity.HasIndex(h => h.TransitionDate);
                entity.HasIndex(h => h.ChangedByUserId);
                entity.HasIndex(h => h.ToState);
                
                // Configure JSON serialization for Metadata
                entity.Property(h => h.Metadata)
                    .HasColumnType("jsonb");
            });
            
            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                
                // Property configurations
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(255);
                entity.Property(u => u.FubApiKey).HasMaxLength(255);
                entity.Property(u => u.Role).HasMaxLength(50);
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                
                // Configure enums or complex types if any
                
                // Indexes
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.OrganizationId);
                entity.HasIndex(u => u.IsActive);
                
                // Relationships
                // Relationship with Organization (as a member)
                entity.HasOne(u => u.Organization)
                    .WithMany(o => o.Users)
                    .HasForeignKey(u => u.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Relationship with OwnedOrganizations (as an owner)
                entity.HasMany(u => u.OwnedOrganizations)
                    .WithOne(o => o.OwnerUser)
                    .HasForeignKey(o => o.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Configure owned types if any
                
                // Configure table name and schema if needed
                entity.ToTable("Users");
                
                // Configure navigation properties
                entity.HasMany(u => u.EmailAddresses)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(u => u.DeviceTokens)
                    .WithOne(dt => dt.User)
                    .HasForeignKey(dt => dt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(u => u.PhoneNumbers)
                    .WithOne(pn => pn.User)
                    .HasForeignKey(pn => pn.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(u => u.OwnedContacts)
                    .WithOne(c => c.Owner)
                    .HasForeignKey(c => c.OwnerId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasMany(u => u.CreatedInteractions)
                    .WithOne(i => i.CreatedBy)
                    .HasForeignKey(i => i.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasMany(u => u.CreatedAgents)
                    .WithOne(a => a.CreatedBy)
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasMany(u => u.SubscriptionAssignments)
                    .WithOne(usa => usa.User)
                    .HasForeignKey(usa => usa.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(u => u.AssignedTasks)
                    .WithOne(t => t.AssignedTo)
                    .HasForeignKey(t => t.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            
            // Organization entity configuration
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(o => o.Id);
                
                // Property configurations
                entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
                entity.Property(o => o.Email).IsRequired().HasMaxLength(200);
                entity.Property(o => o.Website).HasMaxLength(255);
                entity.Property(o => o.PhoneNumber).HasMaxLength(50);
                entity.Property(o => o.LogoUrl).HasMaxLength(500);
                entity.Property(o => o.TimeZone).HasMaxLength(100);
                entity.Property(o => o.Locale).HasMaxLength(20);
                entity.Property(o => o.Currency).HasMaxLength(3);
                entity.Property(o => o.IndustryCode).HasMaxLength(100);
                entity.Property(o => o.IsActive).HasDefaultValue(true);
                
                // Indexes
                entity.HasIndex(o => o.Name);
                entity.HasIndex(o => o.Email);
                entity.HasIndex(o => o.IsActive);
                entity.HasIndex(o => o.OwnerUserId);
                
                // Relationships
                // Relationship with OwnerUser
                entity.HasOne(o => o.OwnerUser)
                    .WithMany(u => u.OwnedOrganizations)
                    .HasForeignKey(o => o.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Relationship with Users
                entity.HasMany(o => o.Users)
                    .WithOne(u => u.Organization)
                    .HasForeignKey(u => u.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Relationship with Contacts
                entity.HasMany(o => o.Contacts)
                    .WithOne(c => c.Organization)
                    .HasForeignKey(c => c.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Relationship with Interactions
                entity.HasMany(o => o.Interactions)
                    .WithOne(i => i.Organization)
                    .HasForeignKey(i => i.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Relationship with Agents
                entity.HasMany(o => o.Agents)
                    .WithOne(a => a.Organization)
                    .HasForeignKey(a => a.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Relationship with SubscriptionPlans
                entity.HasMany(o => o.SubscriptionPlans)
                    .WithOne(sp => sp.Organization)
                    .HasForeignKey(sp => sp.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Configure table name and schema if needed
                entity.ToTable("Organizations");
            });
            
            // Configure many-to-many relationships if any
            
            // Configure owned types if any
            
            // Configure complex types if any
            
            // Configure value conversions if any
            
            // Configure global query filters if any
            
            // Interaction entity configuration
            modelBuilder.Entity<Interaction>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Type).HasMaxLength(50);
                entity.Property(i => i.Direction).HasMaxLength(20);
                entity.Property(i => i.Status).HasMaxLength(20);
                entity.Property(i => i.Priority).HasMaxLength(20);
                entity.Property(i => i.Subject).HasMaxLength(500);
                entity.Property(i => i.Channel).HasMaxLength(50);
                entity.Property(i => i.PrivacyLevel).HasMaxLength(20);
                
                // Indexes
                entity.HasIndex(i => i.ContactId);
                entity.HasIndex(i => i.OrganizationId);
                entity.HasIndex(i => i.CreatedAt);
                entity.HasIndex(i => i.Type);
                entity.HasIndex(i => i.Status);
            });
            
            #endregion
            
            // DeviceToken entity configuration
            modelBuilder.Entity<DeviceToken>(entity =>
            {
                entity.HasKey(dt => dt.Id);
                
                entity.Property(dt => dt.DeviceId).IsRequired().HasMaxLength(255);
                entity.Property(dt => dt.Token).IsRequired().HasMaxLength(500);
                entity.Property(dt => dt.Platform).IsRequired().HasMaxLength(50);
                entity.Property(dt => dt.DeviceName).HasMaxLength(100);
                entity.Property(dt => dt.DeviceModel).HasMaxLength(100);
                entity.Property(dt => dt.OsVersion).HasMaxLength(50);
                entity.Property(dt => dt.AppVersion).HasMaxLength(50);
                entity.Property(dt => dt.IsActive).HasDefaultValue(true);
                
                // Indexes
                entity.HasIndex(dt => dt.UserId);
                entity.HasIndex(dt => dt.DeviceId);
                entity.HasIndex(dt => dt.Token).IsUnique();
                entity.HasIndex(dt => dt.Platform);
                entity.HasIndex(dt => dt.IsActive);
                
                // Relationship with User
                entity.HasOne(dt => dt.User)
                    .WithMany(u => u.DeviceTokens)
                    .HasForeignKey(dt => dt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // EmmaAnalysis entity configuration
            modelBuilder.Entity<EmmaAnalysis>(entity =>
            {
                entity.HasKey(ea => ea.Id);
                
                // Properties
                entity.Property(ea => ea.LeadStatus).IsRequired().HasMaxLength(50);
                entity.Property(ea => ea.RecommendedStrategy).IsRequired().HasMaxLength(500);
                entity.Property(ea => ea.FollowupGuidance).HasMaxLength(2000);
                entity.Property(ea => ea.ConfidenceScore).HasColumnType("smallint");
                entity.Property(ea => ea.ModelVersion).HasMaxLength(50);
                
                // Indexes
                entity.HasIndex(ea => ea.MessageId);
                entity.HasIndex(ea => ea.LeadStatus);
                entity.HasIndex(ea => ea.CreatedAt);
                
                // Relationships
                entity.HasOne(ea => ea.Message)
                    .WithMany(m => m.Analyses)
                    .HasForeignKey(ea => ea.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Configure the one-to-many relationship with EmmaTask
                entity.HasMany(ea => ea.TasksList)
                    .WithOne(t => t.Analysis)
                    .HasForeignKey(t => t.AnalysisId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Configure the one-to-many relationship with AgentAssignment
                entity.HasMany(ea => ea.AgentAssignments)
                    .WithOne(aa => aa.Analysis)
                    .HasForeignKey(aa => aa.AnalysisId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Configure the JSON serialization for ComplianceFlags
                entity.Property(ea => ea.ComplianceFlags)
                    .HasConversion(
                        v => string.Join(',', v ?? new List<string>()),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );
            });
            
            // Interaction entity configuration
            modelBuilder.Entity<Interaction>(entity =>
            {
                entity.HasKey(i => i.Id);
                
                // Properties
                entity.Property(i => i.Type).IsRequired().HasMaxLength(50);
                entity.Property(i => i.Direction).IsRequired().HasMaxLength(20);
                entity.Property(i => i.Status).IsRequired().HasMaxLength(50);
                entity.Property(i => i.Priority).IsRequired().HasMaxLength(20);
                entity.Property(i => i.Subject).HasMaxLength(500);
                entity.Property(i => i.PrivacyLevel).HasMaxLength(20);
                entity.Property(i => i.Channel).IsRequired().HasMaxLength(50);
                entity.Property(i => i.SentimentLabel).HasMaxLength(20);
                entity.Property(i => i.Confidentiality).HasMaxLength(20);
                entity.Property(i => i.RetentionPolicy).HasMaxLength(100);
                
                // Indexes
                entity.HasIndex(i => i.OrganizationId);
                entity.HasIndex(i => i.ContactId);
                entity.HasIndex(i => i.ParentInteractionId);
                entity.HasIndex(i => i.ThreadId);
                entity.HasIndex(i => i.Type);
                entity.HasIndex(i => i.Status);
                entity.HasIndex(i => i.Priority);
                entity.HasIndex(i => i.CreatedAt);
                
                // Relationships
                entity.HasOne(i => i.Organization)
                    .WithMany()
                    .HasForeignKey(i => i.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(i => i.Contact)
                    .WithMany(c => c.Interactions)
                    .HasForeignKey(i => i.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(i => i.ParentInteraction)
                    .WithMany(i => i.ChildInteractions)
                    .HasForeignKey(i => i.ParentInteractionId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configure JSON serialization for complex properties
                entity.Property(i => i.AiMetadata)
                    .HasColumnType("jsonb");
                    
                entity.Property(i => i.ChannelData)
                    .HasColumnType("jsonb");
                    
                entity.Property(i => i.ExternalIds)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions()) ?? new()
                    );
                    
                entity.Property(i => i.CustomFields)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, new JsonSerializerOptions()) ?? new()
                    );
                    
                entity.Property(i => i.Metadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, new JsonSerializerOptions()) ?? new()
                    );
                    
                // Configure one-to-many relationship with Message
                entity.HasMany(i => i.Messages)
                    .WithOne(m => m.Interaction)
                    .HasForeignKey(m => m.InteractionId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Configure one-to-many relationship with TaskItem
                entity.HasMany(i => i.Tasks)
                    .WithOne(t => t.Interaction)
                    .HasForeignKey(t => t.InteractionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Apply any additional configurations from IEntityTypeConfiguration<T> implementations
            // This allows for cleaner separation of concerns by moving configurations to separate files
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
            
            // Configure the Agent entity
            modelBuilder.Entity<Agent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.AgentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Configuration).HasMaxLength(4000);
                entity.Property(e => e.Version).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
                entity.Property(e => e.LastHeartbeat).IsRequired(false);
                
                // Relationships
                entity.HasOne(a => a.Organization)
                    .WithMany(o => o.Agents)
                    .HasForeignKey(a => a.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(a => a.CreatedBy)
                    .WithMany(u => u.CreatedAgents)
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Configure JSON columns
                entity.Property(a => a.Capabilities)
                    .HasConversion(
                        v => string.Join(',', v ?? new List<string>()),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );
                    
                entity.Property(a => a.Metadata)
                    .HasColumnType("jsonb");
                
                entity.HasIndex(a => a.AgentType);
                entity.HasIndex(a => a.OrganizationId);
                entity.HasIndex(a => a.CreatedById);
                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => a.LastHeartbeat);
            });
            
            // Configure the User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FubApiKey).HasMaxLength(255);
                entity.Property(e => e.ProfileImageUrl).HasMaxLength(500);
                entity.Property(e => e.TimeZone).HasMaxLength(100);
                entity.Property(e => e.Locale).HasMaxLength(10);
                entity.Property(e => e.LastLoginAt).IsRequired(false);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                
                // Relationships
                entity.HasOne(u => u.Organization)
                    .WithMany(o => o.Users)
                    .HasForeignKey(u => u.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Configure owned entity types
                entity.OwnsMany(u => u.PhoneNumbers, p =>
                {
                    p.WithOwner().HasForeignKey("UserId");
                    p.Property<Guid>("Id").ValueGeneratedOnAdd();
                    p.HasKey("Id");
                    p.Property(pn => pn.PhoneNumber).IsRequired().HasMaxLength(20);
                    p.Property(pn => pn.Type).HasMaxLength(50);
                    p.Property(pn => pn.IsPrimary).HasDefaultValue(false);
                    p.Property(pn => pn.IsVerified).HasDefaultValue(false);
                    p.Property(pn => pn.VerifiedAt).IsRequired(false);
                });
                
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.OrganizationId);
                entity.HasIndex(u => u.IsActive);
            });
            
            // Configure UserPhoneNumber entity
            modelBuilder.Entity<UserPhoneNumber>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(pn => pn.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(pn => pn.Type).HasMaxLength(50);
                entity.Property(pn => pn.IsPrimary).HasDefaultValue(false);
                entity.Property(pn => pn.IsVerified).HasDefaultValue(false);
                entity.Property(pn => pn.VerifiedAt).IsRequired(false);
                
                // Relationships
                entity.HasOne(pn => pn.User)
                    .WithMany(u => u.PhoneNumbers)
                    .HasForeignKey(pn => pn.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasIndex(pn => pn.UserId);
                entity.HasIndex(pn => pn.PhoneNumber);
                entity.HasIndex(pn => pn.IsPrimary);
            });
            
            // Configure UserSubscriptionAssignment entity
            modelBuilder.Entity<UserSubscriptionAssignment>(entity =>
            {
                entity.HasKey(usa => new { usa.UserId, usa.SubscriptionId });
                
                entity.Property(usa => usa.AssignedAt).IsRequired();
                entity.Property(usa => usa.ExpiresAt).IsRequired(false);
                
                // Relationships
                entity.HasOne(usa => usa.User)
                    .WithMany(u => u.SubscriptionAssignments)
                    .HasForeignKey(usa => usa.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(usa => usa.Subscription)
                    .WithMany(s => s.UserAssignments)
                    .HasForeignKey(usa => usa.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasIndex(usa => new { usa.UserId, usa.IsActive });
                entity.HasIndex(usa => usa.ExpiresAt);
            });
            
            // Configure PasswordResetToken entity
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(prt => prt.Token).IsRequired().HasMaxLength(100);
                entity.Property(prt => prt.CreatedAt).IsRequired();
                entity.Property(prt => prt.UsedAt).IsRequired(false);
                
                // Relationships
                entity.HasOne(prt => prt.User)
                    .WithMany()
                    .HasForeignKey(prt => prt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasIndex(prt => prt.Token).IsUnique();
                entity.HasIndex(prt => prt.UserId);
                entity.HasIndex(prt => prt.UsedAt);
            });
            
            // Configure DeviceToken entity
            modelBuilder.Entity<DeviceToken>(entity =>
            {
                entity.HasKey(dt => new { dt.UserId, dt.DeviceId });
                
                entity.Property(dt => dt.Token).IsRequired().HasMaxLength(500);
                
                // Relationships
                entity.HasOne(dt => dt.User)
                    .WithMany(u => u.DeviceTokens)
                    .HasForeignKey(dt => dt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure the Interaction entity
            modelBuilder.Entity<Interaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Direction).HasMaxLength(50);
                entity.Property(e => e.Channel).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Priority).HasMaxLength(20);
                entity.Property(e => e.Subject).HasMaxLength(500);
                entity.Property(e => e.Summary).HasMaxLength(2000);
                entity.Property(e => e.PrivacyLevel).HasMaxLength(50);
                entity.Property(e => e.Confidentiality).HasMaxLength(50);
                entity.Property(e => e.SentimentLabel).HasMaxLength(50);
                entity.Property(e => e.VectorEmbedding).HasColumnType("vector(1536)"); // For OpenAI embeddings
                
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Configure relationships
                entity.HasOne(i => i.Contact)
                    .WithMany(c => c.Interactions)
                    .HasForeignKey(i => i.ContactId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(i => i.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(i => i.AssignedToUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(i => i.AssignedToAgent)
                    .WithMany()
                    .HasForeignKey(i => i.AssignedToAgentId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(i => i.Organization)
                    .WithMany()
                    .HasForeignKey(i => i.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Configure owned entity types
                entity.OwnsMany(i => i.Participants, p =>
                {
                    p.WithOwner().HasForeignKey("InteractionId");
                    p.Property<Guid>("Id").ValueGeneratedOnAdd();
                    p.HasKey("Id");
                });
                
                entity.OwnsMany(i => i.RelatedEntities, r =>
                {
                    r.WithOwner().HasForeignKey("InteractionId");
                    r.Property<Guid>("Id").ValueGeneratedOnAdd();
                    r.HasKey("Id");
                });
                
                // Configure JSON columns
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => string.Join(',', v ?? new List<string>()),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );
                    
                entity.Property(e => e.CustomFields)
                    .HasColumnType("jsonb");
                    
                entity.Property(e => e.AiMetadata)
                    .HasColumnType("jsonb");
                    
                // Configure indexes
                entity.HasIndex(i => i.ContactId);
                entity.HasIndex(i => i.AssignedToId);
                entity.HasIndex(i => i.OrganizationId);
                entity.HasIndex(i => i.Type);
                entity.HasIndex(i => i.Status);
                entity.HasIndex(i => i.Priority);
                entity.HasIndex(i => i.Channel);
                entity.HasIndex(i => i.CreatedAt);
                entity.HasIndex(i => i.UpdatedAt);
                entity.HasIndex(i => i.FollowUpBy);
                entity.HasIndex(i => i.IsDeleted);
            });
            
            // Configure ActionItem entity
            modelBuilder.Entity<ActionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Priority).HasMaxLength(20);
                entity.Property(e => e.DueDate).IsRequired(false);
                
                // Configure relationships
                entity.HasOne(ai => ai.Interaction)
                    .WithMany(i => i.ActionItems)
                    .HasForeignKey(ai => ai.InteractionId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(ai => ai.AssignedTo)
                    .WithMany()
                    .HasForeignKey(ai => ai.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(ai => ai.CreatedBy)
                    .WithMany()
                    .HasForeignKey(ai => ai.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                // Configure indexes
                entity.HasIndex(ai => ai.InteractionId);
                entity.HasIndex(ai => ai.AssignedToId);
                entity.HasIndex(ai => ai.Status);
                entity.HasIndex(ai => ai.Priority);
                entity.HasIndex(ai => ai.DueDate);
                entity.HasIndex(ai => ai.IsCompleted);
            });
            
            // Configure Participant entity (owned by Interaction)
            modelBuilder.Entity<Participant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Email).HasMaxLength(200);
                entity.Property(p => p.Phone).HasMaxLength(50);
                entity.Property(p => p.Role).HasMaxLength(100);
                entity.Property(p => p.ExternalId).HasMaxLength(100);
                
                // Configure indexes
                entity.HasIndex(p => p.Email);
                entity.HasIndex(p => p.ExternalId);
            });
            
            // Configure RelatedEntity (owned by Interaction)
            modelBuilder.Entity<RelatedEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.Property(r => r.Type).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
                entity.Property(r => r.Role).HasMaxLength(100);
                entity.Property(r => r.ExternalId).HasMaxLength(100);
                
                // Configure indexes
                entity.HasIndex(r => r.Type);
                entity.HasIndex(r => r.ExternalId);
            });
        }
    }
}
