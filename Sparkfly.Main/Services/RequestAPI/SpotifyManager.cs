using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Sparkfly.Main.Services.RequestApi;

public class SpotifyManager
{
    private const string _API_ADDRESS = "https://api.spotify.com/v1";
    private const string _REDIRECT_URI = "https://localhost:5001/validate";
    private const string _SCOPES = "user-read-private user-read-currently-playing user-modify-playback-state";  // TODO: use String.Join

    private static readonly string _CLIENT_ID;
    private static readonly string _CLIENT_SECRET;
    private static readonly HttpClient _httpClient; // NOTE: being static means only one header... TODO: make this non-static and call SetBearerAuthHeader() in every request

    public struct Tokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public Tokens(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }
    }

    static SpotifyManager()
    {
        string? clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        string? clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");

        if (clientId is null)
            throw new RequestApiException("Environment Variable 'SPOTIFY_CLIENT_ID' was not found.");
        if (clientSecret is null)
            throw new RequestApiException("Environment Variable 'SPOTIFY_CLIENT_SECRET' was not found.");

        _CLIENT_ID = clientId;
        _CLIENT_SECRET = clientSecret;

        _httpClient = new();
    }

    private void SetBasicAuthHeader() => _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_CLIENT_ID}:{_CLIENT_SECRET}"))}");
    private void SetBearerAuthHeader(string accessToken) => _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public Uri RequestUserAuthorizationUri(string state)
    {
        KeyValuePair<string, string?>[] parameters = new[]
        {
            new KeyValuePair<string, string?>("client_id", _CLIENT_ID),
            new KeyValuePair<string, string?>("response_type", "code"),
            new KeyValuePair<string, string?>("redirect_uri", _REDIRECT_URI),
            new KeyValuePair<string, string?>("scope", _SCOPES),
            new KeyValuePair<string, string?>("state", state)
        };

        return new Uri("https://accounts.spotify.com/authorize" + QueryString.Create(parameters).ToString());
    }

    public async Task<Tokens> RequestAccessAndRefreshTokensAsync(string authCode, string originalStateCode, string returnedStateCode)
    {
        if (originalStateCode != returnedStateCode)
            throw new RequestApiException("Invalid state code returned by the server.");    // TODO: make this catchable

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string, string?>("code", authCode),
            new KeyValuePair<string, string?>("redirect_uri", _REDIRECT_URI)
        });

        SetBasicAuthHeader();
        using HttpResponseMessage httpResponse = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);

        httpResponse.EnsureSuccessStatusCode();

        using JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

        string? accessToken = jsonResponse.RootElement.GetProperty("access_token").GetString();
        string? refreshToken = jsonResponse.RootElement.GetProperty("refresh_token").GetString();

        if (accessToken is null)
            throw new RequestApiException("Returned access token is null.");

        if (refreshToken is null)
            throw new RequestApiException("Returned refresh token is null.");

        SetBearerAuthHeader(accessToken);

        return new Tokens(accessToken, refreshToken);
    }

    public async Task<string> RefreshAccessTokenAsync(string refreshToken)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string?>("grant_type", "refresh_token"),
            new KeyValuePair<string, string?>("refresh_token", refreshToken)
        });

        SetBasicAuthHeader();
        using HttpResponseMessage httpResponse = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);

        httpResponse.EnsureSuccessStatusCode();

        using JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

        string? newAccessToken = jsonResponse.RootElement.GetProperty("access_token").GetString();

        if (newAccessToken is null)
            throw new RequestApiException("Returned access token is null.");

        SetBearerAuthHeader(newAccessToken);

        return newAccessToken;
    }

    public async Task<Track?> GetCurrentlyPlayingAsync()
    {
        using HttpResponseMessage httpResponse = await _httpClient.GetAsync(_API_ADDRESS + "/me/player/currently-playing");

        httpResponse.EnsureSuccessStatusCode();

        if (httpResponse.StatusCode == HttpStatusCode.OK)
        {
            using JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

            Track currentTrack = GetTrackFromJson(jsonResponse.RootElement.GetProperty("item"));

            currentTrack.ProgressMs = jsonResponse.RootElement.GetProperty("progress_ms").GetInt32();

            return currentTrack;
        }
        else
            return null;
    }

    public async Task<List<Track>> SearchTracksAsync(string searchFor)
    {
        KeyValuePair<string, string?>[] parameters = new[]
        {
            new KeyValuePair<string, string?>("q", searchFor),
            new KeyValuePair<string, string?>("type", "track"),
            new KeyValuePair<string, string?>("limit", "10")
        };

        using HttpResponseMessage httpResponse = await _httpClient.GetAsync(_API_ADDRESS + "/search" + QueryString.Create(parameters));

        httpResponse.EnsureSuccessStatusCode();

        JsonDocument jsonResponse = JsonDocument.Parse(await httpResponse.Content.ReadAsStringAsync());

        List<Track> searchedTracks = new();
        foreach (JsonElement item in jsonResponse.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray())
            searchedTracks.Add(GetTrackFromJson(item));

        return searchedTracks;
    }

    public async Task AddToPlaybackQueueAsync(Track track)
    {
        using HttpResponseMessage httpResponse = await _httpClient.PostAsync(_API_ADDRESS + "/me/player/queue" + QueryString.Create("uri", $"spotify:track:{track.SongId}"), null);

        httpResponse.EnsureSuccessStatusCode();
    }

    private Track GetTrackFromJson(JsonElement item)
    {
        Track track = new();

        track.SongId = item.GetProperty("id").GetString();
        track.SongName = item.GetProperty("name").GetString();
        track.AlbumName = item.GetProperty("album").GetProperty("name").GetString();
        track.DurationMs = item.GetProperty("duration_ms").GetInt32();

        foreach (JsonElement cover in item.GetProperty("album").GetProperty("images").EnumerateArray())
            track.CoverSizesUrl.Add(cover.GetProperty("url").GetString());

        foreach (JsonElement artist in item.GetProperty("artists").EnumerateArray())
            track.ArtistsNames.Add(artist.GetProperty("name").GetString());

        return track;
    }
}

