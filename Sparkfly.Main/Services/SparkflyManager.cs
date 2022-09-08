using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;
using Sparkfly.Main.Services.RequestApi;
using System.Net;
using static Sparkfly.Main.Services.TimerManager;

namespace Sparkfly.Main.Services;

public class SparkflyManager
{
    #region Attributes and Constructor
    private readonly SpotifyManager _spotifyManager;
    private readonly VotingManager _votingManager;
    private readonly TimerManager _timerManager;
    private readonly NavigationManager _navigationManager;
    private readonly ProtectedSessionStorage _currentSession;

    private int _loopPeriodInSeconds = 0;

    private SpotifyManager.Tokens _tokens;
    public Track CurrentlyPlayingTrack { get; set; }

    public SparkflyManager(NavigationManager navigationManager, ProtectedSessionStorage protectedSession, TimerManager timerManager)
    {
        _navigationManager = navigationManager;
        _currentSession = protectedSession;
        _timerManager = timerManager;

        _spotifyManager = new SpotifyManager();
        _votingManager = new VotingManager(_currentSession);

        CurrentlyPlayingTrack = new Track().MakeThisDummy();
    }
    #endregion

    #region Spotify Methods
    public Uri SpotifySignInUri(string state) => _spotifyManager.RequestUserAuthorizationUri(state);
    public async Task SpotifyRequestTokensAsync(string authCode, string originalStateCode, string returnedStateCode)
    {
        try
        {
            _tokens = await _spotifyManager.RequestAccessAndRefreshTokensAsync(authCode, originalStateCode, returnedStateCode);
        }
        catch (HttpRequestException e)
        {
            await HandleHttpExceptionAsync(e);
        }
    }
    public async Task SpotifyRefreshAccessTokenAsync() => _tokens.AccessToken = await _spotifyManager.RefreshAccessTokenAsync(_tokens.RefreshToken);
    private async Task<Track> SpotifyGetCurrentlyPlayingAsync() => await _spotifyManager.GetCurrentlyPlayingAsync() ?? new Track().MakeThisDummy();
    public async Task<List<Track>> SpotifySearchTracksAsync(string searchFor) => await _spotifyManager.SearchTracksAsync(searchFor);
    private async Task SpotifyAddToPlaybackQueueAsync(Track track) => await _spotifyManager.AddToPlaybackQueueAsync(track);
    #endregion

    #region Voting Queue Methods
    public async Task<Queue<Vote>?> GetVotingQueueAsync() => await _votingManager.GetQueueAsync();
    public async Task EnqueueVoteAsync(Track track)
    {
        string? clientId = (await GetThisClientAsync())?.ClientId;

        if (clientId is not null)
            await _votingManager.EnqueueVoteAsync(track, clientId);
    }
    public async Task<Track?> DequeueVoteAsync() => await _votingManager.DequeueVoteAsync();    // TODO: change return type to Vote
    public async Task<bool> RemoveVoteAsync(Track track)
    {
        string? clientId = (await GetThisClientAsync())?.ClientId;

        if (clientId is null)
            return false;

        return await _votingManager.RemoveVoteAsync(track, clientId);
    }
    public async Task<Vote?> PeekVotingQueue()  // TODO: use TryPeek instead and add Async to method name
    {
        try
        {
            return (await GetVotingQueueAsync())?.Peek();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
    #endregion

    #region Timer Methods
    public void SubscribeToTimerEvent(TimeElapsedEventHandler method) => _timerManager.TimeElapsed += method;
    public void UnsubscribeToTimerEvent(TimeElapsedEventHandler method) => _timerManager.TimeElapsed -= method;
    public void StartTimer(int seconds = 15)
    {
        if (_timerManager.HasStarted)
            StopTimer();

        SubscribeToTimerEvent(OnTimerElapsedAsync);

        _timerManager.Start(seconds);

        _loopPeriodInSeconds = seconds;
    }

    public void StopTimer()
    {
        UnsubscribeToTimerEvent(OnTimerElapsedAsync);

        _timerManager.Stop();
    }

    private async void OnTimerElapsedAsync(object source, EventArgs args)
    {
        Track newestTrack = await SpotifyGetCurrentlyPlayingAsync();
        Track? nextTrack = (await PeekVotingQueue())?.VotedTrack;

        if (newestTrack.SongId != CurrentlyPlayingTrack.SongId)
        {
            CurrentlyPlayingTrack = newestTrack;

            if (newestTrack.SongId == nextTrack?.SongId)
                nextTrack = await DequeueVoteAsync();
        }

        if (nextTrack is not null && (newestTrack.DurationMs - newestTrack.ProgressMs) < (_loopPeriodInSeconds * 1000))
            await SpotifyAddToPlaybackQueueAsync(nextTrack);

        // TODO: else add a recommended track
    }
    #endregion

    #region Client Methods
    public async Task<Client?> GetThisClientAsync() => (await _currentSession.GetAsync<Client>("this_client")).Value;
    public async Task SetThisClientAsync(Client client) => await _currentSession.SetAsync("this_client", client);
    public async Task<string?> GetThisClientNameAsync() => (await GetThisClientAsync())?.ClientName;    // FIXME: remove
    public async Task SetThisClientNameAsync(string name)
    {
        Client? client = await GetThisClientAsync();

        if (client is not null)
        {
            client.ClientName = name;
            await SetThisClientAsync(client);
        }
    }
    #endregion

    #region Other Methods
    public async Task HandleHttpExceptionAsync(HttpRequestException exception)
    {
        try
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
                await SpotifyRefreshAccessTokenAsync();
            else
                _navigationManager.NavigateTo("/unhandled-error" + QueryString.Create("message", exception.Message));
        }
        catch (HttpRequestException)
        {
            _navigationManager.NavigateTo("/unhandled-error" + QueryString.Create("message", exception.Message));
        }
    }
    #endregion
}
