namespace Sparkfly.Main.Services.RequestApi;

public class RequestApiException : Exception
{
    public RequestApiException() { }
    public RequestApiException(string message) : base(message) { }
    public RequestApiException(string message, Exception innerException) : base(message, innerException) { }
}
