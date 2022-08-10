using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;
using Sparkfly.Main.Services.RequestApi;
using System.Net;

namespace Sparkfly.Main.Services;

public class SparkflyManager
{
    #region Attributes and Constructor
    private readonly SpotifyManager _spotify;
    private readonly VotingManager _votingManager;
    private readonly TimerManager _timerManager;
    private readonly NavigationManager _navigationManager;
    private readonly ProtectedSessionStorage _currentSession;

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
    public async Task<Track> SpotifyGetCurrentlyPlayingAsync() => await _spotify.GetCurrentlyPlayingAsync();
    public async Task<List<Track>> SpotifySearchTracksAsync(string searchFor) => await _spotify.SearchTracksAsync(searchFor);
    private async Task SpotifyAddToPlaybackQueueAsync(Track track) => await _spotify.AddToPlaybackQueueAsync(track);
    #endregion

    #region Queue Methods
    public async Task<Queue<Vote>?> GetVotingQueueAsync() => await _votingManager.GetQueueAsync();
    public async Task DequeueVoteAsync() => await _votingManager.DequeueVoteAsync();

    public async Task EnqueueVoteAsync(Track track)
    {
        if ((await GetVotingQueueAsync()) is null)  // TODO: check for !Any()
            await SpotifyAddToPlaybackQueueAsync(track);

        await _votingManager.EnqueueVoteAsync(track);
    }
    #endregion

    #region Timer Methods
    public void StartTimer(int seconds = 15)
    {
        if (_timerManager.HasStarted)
            StopTimer();

        _timerManager.TimeElapsed += OnTimerElapsedAsync;
        _timerManager.Start(seconds);
    }

    public void StopTimer()
    {
        _timerManager.TimeElapsed -= OnTimerElapsedAsync;
        _timerManager.Stop();
    }

    public async void OnTimerElapsedAsync(object source, EventArgs args)
    {
        if ((await SpotifyGetCurrentlyPlayingAsync()).SongId == (await GetVotingQueueAsync())?.Peek().VotedTrack?.SongId)
        {
            await DequeueVoteAsync();

            Track? nextTrack = (await GetVotingQueueAsync())?.Peek().VotedTrack;

            if (nextTrack is not null)
                await SpotifyAddToPlaybackQueueAsync(nextTrack);
            // call await InvokeAsync(StateHasChanged) ??
            // TODO: else add a recommended track
        }
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
