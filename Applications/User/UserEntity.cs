using System.ComponentModel.DataAnnotations;

namespace Anma.Applications.User;

public class UserEntity
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Password { get; set; } = "";
}
