namespace Sparkfly.Main.RequestAPI;

public class RequestAPIException : Exception
{
    public RequestAPIException() { }
    public RequestAPIException(string message) : base(message) { }
    public RequestAPIException(string message, Exception innerException) : base(message, innerException) { }
}
