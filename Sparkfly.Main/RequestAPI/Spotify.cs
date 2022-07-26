using Microsoft.AspNetCore.Components;

namespace Sparkfly.Main.RequestAPI;

public class Spotify
{
    private const string _BASE_ADDRESS = "https://api.spotify.com";
    private const string _SCOPES = "user-read-private";

    private static readonly string _CLIENT_ID;
    private static readonly string _CLIENT_SECRET;

    private readonly NavigationManager _navigationManager;
    private string? _authCode;

    static Spotify()
    {
        string? clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        string? clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");

        if (clientId is null)
            throw new ArgumentNullException(nameof(clientId), "Environment Variable 'SPOTIFY_CLIENT_ID' was not found.");
        if (clientSecret is null)
            throw new ArgumentNullException(nameof(clientSecret), "Environment Variable 'SPOTIFY_CLIENT_SECRET' was not found.");

        _CLIENT_ID = clientId;
        _CLIENT_SECRET = clientSecret;
    }

    public Spotify(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public void GetUserAuthorization(string state)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            KeyValuePair.Create("client_id", _CLIENT_ID),
            KeyValuePair.Create("response_type", "code"),
            KeyValuePair.Create("redirect_uri", "https://localhost:5001/"),
            KeyValuePair.Create("scopes", _SCOPES),
            KeyValuePair.Create("state", state)
        };

        _navigationManager.NavigateTo("https://accounts.spotify.com/authorize" + QueryString.Create(parameters).ToString());
    }

    public void SetAuthCode(string authCode) => _authCode = authCode;
}

