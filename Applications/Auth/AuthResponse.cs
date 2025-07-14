namespace Anma.Applications.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public int ExpireIn { get; set; } = 60;
}
