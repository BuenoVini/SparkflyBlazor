using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Sparkfly.Main.RequestAPI;

public class Spotify
{
    private const string _API_ADDRESS = "https://api.spotify.com";
    private const string _REDIRECT_URI = "https://localhost:5001/";
    private const string _SCOPES = "user-read-private";

    private static readonly string _CLIENT_ID;
    private static readonly string _CLIENT_SECRET;
    private static readonly HttpClient _httpClient = new();

    private readonly NavigationManager _navigationManager;
    private readonly ProtectedSessionStorage _currentSession;

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

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_CLIENT_ID}:{_CLIENT_SECRET}"))}");
    }

    public Spotify(NavigationManager navigationManager, ProtectedSessionStorage session)
    {
        _navigationManager = navigationManager;
        _currentSession = session;
    }

    public async Task RequestUserAuthorization()
    {
        string state = new Random().Next().ToString();

        KeyValuePair<string, string?>[] parameters = new[]
        {
            new KeyValuePair<string, string?>("client_id", _CLIENT_ID),
            new KeyValuePair<string, string?>("response_type", "code"),
            new KeyValuePair<string, string?>("redirect_uri", _REDIRECT_URI),
            new KeyValuePair<string, string?>("scopes", _SCOPES),
            new KeyValuePair<string, string?>("state", state)
        };

        await _currentSession.SetAsync("state", state);

        _navigationManager.NavigateTo("https://accounts.spotify.com/authorize" + QueryString.Create(parameters).ToString());
    }

    public async Task SetAuthCode(string authCode, string stateCode)
    {
        if (stateCode == (await _currentSession.GetAsync<string>("state")).Value)
        {
            _authCode = authCode;
            await RequestAccessAndRefreshToken();
        }
    }

    private async Task RequestAccessAndRefreshToken()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string, string?>("code", _authCode),
            new KeyValuePair<string, string?>("redirect_uri", _REDIRECT_URI)
        });

        using HttpResponseMessage httpResponse = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);

        if (httpResponse.IsSuccessStatusCode)
        {
            using JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

            string? accessToken, refreshToken;

            accessToken = jsonResponse.RootElement.GetProperty("access_token").GetString();
            refreshToken = jsonResponse.RootElement.GetProperty("refresh_token").GetString();

            if (accessToken is not null && refreshToken is not null)
            {
                // TODO: find a proper way to store the tokens
                await _currentSession.SetAsync("access_token", accessToken);
                await _currentSession.SetAsync("refresh_token", refreshToken);
            }
        }
    }
}

