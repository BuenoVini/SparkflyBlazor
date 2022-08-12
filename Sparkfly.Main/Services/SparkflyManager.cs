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
    private readonly SpotifyManager _spotify;
    private readonly VotingManager _votingManager;
    private readonly TimerManager _timerManager;
    private readonly NavigationManager _navigationManager;
    private readonly ProtectedSessionStorage _currentSession;

    private int _loopPeriodInSeconds = 0;

    public SparkflyManager(NavigationManager navigationManager, ProtectedSessionStorage protectedSession, TimerManager timerManager)
    {
        _navigationManager = navigationManager;
        _currentSession = protectedSession;
        _timerManager = timerManager;

        _spotify = new(_navigationManager, _currentSession);
        _votingManager = new(_currentSession);
    }
    #endregion

    #region Spotify Methods
    public async Task SpotifySignInAsync() => await _spotify.RequestUserAuthorizationAsync();
    public async Task SpotifyRequestTokensAsync(string code, string state) => await _spotify.RequestAccessAndRefreshTokensAsync(code, state);
    public async Task SpotifyRefreshTokenAsync() => await _spotify.RefreshAccessTokenAsync();
    public async Task<Track> SpotifyGetCurrentlyPlayingAsync() => await _spotify.GetCurrentlyPlayingAsync();    // TODO: make here the Dummy
    public async Task<List<Track>> SpotifySearchTracksAsync(string searchFor) => await _spotify.SearchTracksAsync(searchFor);
    private async Task SpotifyAddToPlaybackQueueAsync(Track track) => await _spotify.AddToPlaybackQueueAsync(track);
    #endregion

    #region Queue Methods
    public async Task<Queue<Vote>?> GetVotingQueueAsync() => await _votingManager.GetQueueAsync();
    public async Task DequeueVoteAsync() => await _votingManager.DequeueVoteAsync();
    public async Task<Vote?> PeekVotingQueue()
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

    public async Task EnqueueVoteAsync(Track track)
    {
        //if ((await GetVotingQueueAsync()) is null)  // TODO: check for !Any()
        //    await SpotifyAddToPlaybackQueueAsync(track);

        await _votingManager.EnqueueVoteAsync(track);
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
        Track currentTrack = await SpotifyGetCurrentlyPlayingAsync();
        Track? nextTrack = (await PeekVotingQueue())?.VotedTrack;

        if (nextTrack is not null && (currentTrack.DurationMs - currentTrack.ProgressMs) < (_loopPeriodInSeconds * 1000))
        {
            await SpotifyAddToPlaybackQueueAsync(nextTrack);
        }
        else if (currentTrack.SongId == nextTrack?.SongId)
            await DequeueVoteAsync();
        // TODO: else add a recommended track
    }
    #endregion

    #region Other Methods
    public async Task HandleHttpExceptionAsync(HttpRequestException exception)
    {
        try
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
                await _spotify.RefreshAccessTokenAsync();
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
