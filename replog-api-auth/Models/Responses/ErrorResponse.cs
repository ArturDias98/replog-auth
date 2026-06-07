namespace replog_api_auth.Models.Responses;

public class ErrorResponse
{
    public required string Error { get; set; }
    public required string Message { get; set; }
}
