using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Anma.Applications.Workspace;

namespace Anma.Applications.Notebook;

public class NotebookEntity
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public int WorkspaceId { get; set; }
    public WorkspaceEntity Workspace { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string Content { get; set; } = "{}";
}

