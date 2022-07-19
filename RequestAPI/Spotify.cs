using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace RequestAPI;
public static class Spotify
{
    private const string _BASE_ADDRESS = "https://api.spotify.com";
    private const string _SCOPES = "user-read-private";

    private static string _CLIENT_ID;
    private static string _CLIENT_SECRET;

    private static readonly HttpClient s_client = new();

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

    public static async Task GetUserAuthorization()
    {
        int state = new Random().Next();

        var parameters = new List<KeyValuePair<string, string>>
        { 
            KeyValuePair.Create("client_id", _CLIENT_ID),
            KeyValuePair.Create("response_type", "code"),
            KeyValuePair.Create("redirect_uri", "https://open.spotify.com/"),
            KeyValuePair.Create("scopes", _SCOPES),
            KeyValuePair.Create("state", state.ToString())
        };

        string query = QueryString.Create(parameters).ToString();
        //using HttpResponseMessage response = await s_client.GetAsync("https://accounts.spotify.com/authorize" + query);
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://accounts.spotify.com/authorize" + query,
            UseShellExecute = true
        });

    }
}
