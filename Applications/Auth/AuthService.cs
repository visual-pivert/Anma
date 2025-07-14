using Anma.Applications.User;
using Microsoft.AspNetCore.Identity;

namespace Anma.Applications.Auth;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly UserService _userService;
    private readonly JwtService _jwt;
    private readonly PasswordHasher<UserEntity> _hasher = new();

    public AuthService(AppDbContext db, UserService userService, JwtService jwt)
    {
        _db = db;
        _userService = userService;
        _jwt = jwt;
    }

    public string Authenticate(string username, string password)
    {
        var user = _userService.GetUserByUsername(username);
        var result = _hasher.VerifyHashedPassword(user, user.Password, password);
        if (result == PasswordVerificationResult.Success)
        {
            string access_token = _jwt.GenerateToken(user.Id, user.Username);
            return access_token;
        }
        else
        {
            throw new Exception("Invalid credential");
        }
    }
}
