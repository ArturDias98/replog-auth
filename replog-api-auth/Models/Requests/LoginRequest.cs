namespace replog_api_auth.Models.Requests;

public class LoginRequest
{
    public required string GoogleIdToken { get; set; }
}
