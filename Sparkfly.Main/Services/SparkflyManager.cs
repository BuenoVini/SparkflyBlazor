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
    private readonly TimerManager _timerManager;
    private readonly NavigationManager _navigationManager;

    private int _loopPeriodInSeconds = 0;

    private SpotifyManager.Tokens _tokens;
    public Track CurrentlyPlayingTrack { get; private set; }
    public Queue<Vote> Votes { get; private set; }

    public SparkflyManager(NavigationManager navigationManager, TimerManager timerManager)
    {
        _navigationManager = navigationManager;
        _timerManager = timerManager;

        _spotifyManager = new SpotifyManager();

        CurrentlyPlayingTrack = new Track().MakeThisDummy();
        Votes = new Queue<Vote>();
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
    public void EnqueueVote(Track votedTrack, Client client) => Votes.Enqueue(new Vote(votedTrack, client));
    public Vote? TryDequeueVote() => Votes.TryDequeue(out Vote? dequeuedVote) ? dequeuedVote : null;
    public void RemoveVote(Track track, Client client) => Votes = new (Votes.Where(v => !(v.VotedTrack.SongId == track.SongId && v.Client.Id == client.Id)));
    public Vote? TryPeekVotingQueue() => Votes.TryPeek(out Vote? voteOnTop) ? voteOnTop : null;
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
        Track? nextTrack = TryPeekVotingQueue()?.VotedTrack;

        if (newestTrack.SongId != CurrentlyPlayingTrack.SongId)
        {
            CurrentlyPlayingTrack = newestTrack;

            if (newestTrack.SongId == nextTrack?.SongId)
                nextTrack = TryDequeueVote()?.VotedTrack;
        }

        if (nextTrack is not null && (newestTrack.DurationMs - newestTrack.ProgressMs) < (_loopPeriodInSeconds * 1000))
            await SpotifyAddToPlaybackQueueAsync(nextTrack);

        // TODO: else add a recommended track
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
