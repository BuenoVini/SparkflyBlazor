using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Sparkfly.Main.RequestAPI;

public class Spotify
{
    private const string _API_ADDRESS = "https://api.spotify.com/v1";
    private const string _REDIRECT_URI = "https://localhost:5001/current-playing/";
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
    }

    public Spotify(NavigationManager navigationManager, ProtectedSessionStorage session)
    {
        _navigationManager = navigationManager;
        _currentSession = session;
    }

    private AuthenticationHeaderValue SetBasicAuthHeader() => new AuthenticationHeaderValue("Basic", $"{Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_CLIENT_ID}:{_CLIENT_SECRET}"))}");
    private AuthenticationHeaderValue SetBearerAuthHeader(string accessToken) => new AuthenticationHeaderValue("Bearer", accessToken);

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

            await RequestAccessToken();
        }
    }

    private async Task RequestAccessToken()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string, string?>("code", _authCode),
            new KeyValuePair<string, string?>("redirect_uri", _REDIRECT_URI)
        });

        _httpClient.DefaultRequestHeaders.Authorization = SetBasicAuthHeader();
        using HttpResponseMessage httpResponse = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);

        if (httpResponse.IsSuccessStatusCode)
        {
            using JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

            string? accessToken = jsonResponse.RootElement.GetProperty("access_token").GetString();
            string? refreshToken = jsonResponse.RootElement.GetProperty("refresh_token").GetString();

            // TODO: find a proper way to store the tokens
            if (accessToken is not null && refreshToken is not null)
            {
                await _currentSession.SetAsync("access_token", accessToken);
                await _currentSession.SetAsync("refresh_token", refreshToken);

                _httpClient.DefaultRequestHeaders.Authorization = SetBearerAuthHeader(accessToken);
            }
        }
    }

    private async Task RefreshAccessToken()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string?>("grant_type", "refresh_token"),
            new KeyValuePair<string, string?>("refresh_token", (await _currentSession.GetAsync<string>("refresh_token")).Value)
        });

        _httpClient.DefaultRequestHeaders.Authorization = SetBasicAuthHeader();
        using HttpResponseMessage httpResponse = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);

        if (httpResponse.IsSuccessStatusCode)
        {
            using JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

            string? accessToken = jsonResponse.RootElement.GetProperty("access_token").GetString();

            // TODO: find a proper way to store the tokens
            if (accessToken is not null)
            {
                await _currentSession.SetAsync("access_token", accessToken);

                _httpClient.DefaultRequestHeaders.Authorization = SetBearerAuthHeader(accessToken);
            }
        }
    }

    public async Task<List<Track>?> SearchTracks(string searchFor)
    {
        KeyValuePair<string, string?>[] parameters = new[]
        {
            new KeyValuePair<string, string?>("q", searchFor),
            new KeyValuePair<string, string?>("type", "track"),
            new KeyValuePair<string, string?>("limit", "10")
        };

        using HttpResponseMessage httpResponse = await _httpClient.GetAsync(_API_ADDRESS + "/search" + QueryString.Create(parameters));

        if (httpResponse.IsSuccessStatusCode)
        {
            using JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

            List<Track> searchedTracks = new();
            foreach (JsonElement item in jsonResponse.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray())
            {
                Track track = new();

                track.SongId = item.GetProperty("id").GetString();
                track.SongName = item.GetProperty("name").GetString();
                track.AlbumName = item.GetProperty("album").GetProperty("name").GetString();

                foreach(JsonElement cover in item.GetProperty("album").GetProperty("images").EnumerateArray())
                    track.CoverSizesUrl.Add(cover.GetProperty("url").GetString());

                foreach (JsonElement artist in item.GetProperty("artists").EnumerateArray())
                    track.ArtistsNames.Add(artist.GetProperty("name").GetString());

                searchedTracks.Add(track);
            }

            return searchedTracks;
        }

        return null;
    }
}

