namespace Sparkfly.Main.Services.RequestApi;

public class SpotifyApiException : Exception
{
    public SpotifyApiException() { }
    public SpotifyApiException(string message) : base(message) { }
    public SpotifyApiException(string message, Exception innerException) : base(message, innerException) { }
}
