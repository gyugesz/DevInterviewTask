using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Domain;

namespace Todo.Infrastructure;

public sealed class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TodoItemConfiguration());
    }
}

file sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> b)
    {
        b.ToTable("TodoItem", "app");
        b.HasKey(x => x.Id).IsClustered();

        b.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.Description)
            .HasMaxLength(256);

        b.Property(x => x.Priority)
            .HasConversion<byte>() // enum -> tinyint
            .IsRequired();

        b.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(0)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        b.Property(x => x.IsDone)
            .HasDefaultValue(false);

        b.HasIndex(x => x.Name).HasDatabaseName("IX_TodoItem_Name");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TodoItem_Priority", "[Priority] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_TodoItem_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });
    }
}
