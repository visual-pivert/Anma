using System.ComponentModel.DataAnnotations;
using Anma.Applications.User;

namespace Anma.Applications.Auth;

public class AuthEntity
{
    [Key]
    public int Id { get; set; }

    public string AccessToken { get; set; } = "";

    [Required]
    public UserEntity User { get; set; } = null!; 

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
