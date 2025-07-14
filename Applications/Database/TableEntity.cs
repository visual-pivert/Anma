using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Anma.Applications.Database;

public class TableEntity
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public int DatabaseId { get; set; }
    public DatabaseEntity Database { get; set; } = null!;

    // Données structurées par colonne : chaque clé est un champ, avec type + value + options
    [Column(TypeName = "jsonb")]
    public string ColumnsJson { get; set; } = "{}";
}
