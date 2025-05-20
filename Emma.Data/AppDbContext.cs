using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Transcription> Transcriptions { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<EmmaAnalysis> EmmaAnalyses { get; set; }
    public DbSet<AgentAssignment> AgentAssignments { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<ConversationSummary> ConversationSummaries { get; set; }
    public DbSet<CallMetadata> CallMetadata { get; set; }
    public DbSet<AgentPhoneNumber> AgentPhoneNumbers { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // AgentAssignment is a true entity with a foreign key to EmmaAnalysis
        modelBuilder.Entity<AgentAssignment>()
            .HasKey(a => a.Id);
        modelBuilder.Entity<AgentAssignment>()
            .HasOne(a => a.EmmaAnalysis)
            .WithMany(e => e.AgentAssignments)
            .HasForeignKey(a => a.EmmaAnalysisId)
            .IsRequired();
        modelBuilder.Entity<Conversation>().HasKey(c => c.Id);
        modelBuilder.Entity<Conversation>().HasIndex(c => c.ClientId).IsUnique();
        modelBuilder.Entity<Conversation>().HasIndex(c => c.AgentId);
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
    }
}
