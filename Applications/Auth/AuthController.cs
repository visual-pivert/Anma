using Microsoft.AspNetCore.Mvc;

namespace Anma.Applications.Auth;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{

    private readonly ILogger<AuthController> _logger;
    private readonly AuthService _authService;

    public AuthController (ILogger<AuthController> logger, AuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    [HttpPost]
    public ActionResult<AuthResponse> Login ([FromBody] AuthDto authDto) 
    {
        string username = authDto.Username;
        string password = authDto.Password;

        var access_token = _authService.Authenticate(username, password);
        var expire_in = 60;

        var auth_response = new AuthResponse {AccessToken = access_token, ExpireIn = expire_in};
        _logger.LogInformation("User: {username} is logged in", username);
        return Ok(auth_response);
    }

}
