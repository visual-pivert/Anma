using Microsoft.AspNetCore.Identity;

namespace Anma.Applications.User;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<UserEntity> _hasher = new();

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserEntity> CreateUser(string username, string password)
    {
        var newUser = new UserEntity
        {
            Username = username,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        newUser.Password = _hasher.HashPassword(newUser, password);
        _db.Add(newUser);
        await _db.SaveChangesAsync();
        return newUser;
    }

    public UserEntity GetUserByUsername(string username)
    {
        var user = _db.Users.SingleOrDefault(u => u.Username == username);
        if (user == null) {
            throw new Exception("Invalid credential");
        }
        return user;
    }
}
