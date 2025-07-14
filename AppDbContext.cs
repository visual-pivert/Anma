namespace Anma;

using Anma.Applications.Database;
using Anma.Applications.User;
using Anma.Applications.Auth;
using Anma.Applications.Workspace;
using Anma.Applications.Notebook;

using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<AuthEntity> Auth { get; set; } = null!;
    public DbSet<WorkspaceEntity> Workspaces { get; set; } = null!;
    public DbSet<DatabaseEntity> Databases { get; set; } = null!;
    public DbSet<TableEntity> Tables { get; set; } = null!;
    public DbSet<NotebookEntity> Notebooks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}

