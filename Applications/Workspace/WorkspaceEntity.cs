using System.ComponentModel.DataAnnotations;

namespace Anma.Applications.Workspace;

public class WorkspaceEntity
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Color { get; set; } = "";
    public int OwnerId { get; set; }
    public string Logo { get; set; } = "";
}
