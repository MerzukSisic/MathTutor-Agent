using AiAgents.MathTutorAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Infrastructure;

public class MathTutorDbContext(DbContextOptions<MathTutorDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TopicEdge> TopicEdges => Set<TopicEdge>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<StudentTopicState> StudentTopicStates => Set<StudentTopicState>();
    public DbSet<RevisionScheduleItem> RevisionScheduleItems => Set<RevisionScheduleItem>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<ImageNote> ImageNotes => Set<ImageNote>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Topic - TopicEdge relationships
        modelBuilder.Entity<TopicEdge>()
            .HasOne(e => e.PrerequisiteTopic)
            .WithMany(t => t.Dependents)
            .HasForeignKey(e => e.PrerequisiteTopicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TopicEdge>()
            .HasOne(e => e.DependentTopic)
            .WithMany(t => t.Prerequisites)
            .HasForeignKey(e => e.DependentTopicId)
            .OnDelete(DeleteBehavior.Restrict);

        // SystemSettings - single row
        modelBuilder.Entity<SystemSettings>()
            .HasData(new SystemSettings { Id = 1 });
    }
}