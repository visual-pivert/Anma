using Microsoft.AspNetCore.Mvc;

namespace Anma.Applications.User;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly UserService _userService;

    public UserController(ILogger<UserController> logger, UserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<UserEntity>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        var newUser = await _userService.CreateUser(
            createUserDto.Username,
            createUserDto.Password
        );
        _logger.LogInformation("User: {username} is created", newUser.Username);
        return Ok(newUser);

    }
}
