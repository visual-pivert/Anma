using System.ComponentModel.DataAnnotations;
using Anma.Applications.Workspace;

namespace Anma.Applications.Database;

public class DatabaseEntity {
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public WorkspaceEntity Workspace { get; set; } = null!;
    public int WorkspaceId { get; set; }
}
