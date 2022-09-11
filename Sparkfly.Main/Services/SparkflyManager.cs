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
    public EventHandler? TimerUpdateEvent;

    private readonly SpotifyManager _spotifyManager;
    private readonly TimerManager _timerManager;

    public Vote CurrentlyPlayingVote { get; private set; }
    public Vote? PreviouslyPlayedVote { get; private set; }
    public Queue<Vote> Votes { get; private set; }
    public List<Client> Clients { get; private set; }

    private SpotifyManager.Tokens _tokens;
    private int _loopPeriodInMs = 0;

    public SparkflyManager(TimerManager timerManager)
    {
        _timerManager = timerManager;

        _spotifyManager = new SpotifyManager();

        CurrentlyPlayingVote = MakeDummyVote();
        Votes = new Queue<Vote>();
        Clients = new List<Client>();
    }
    #endregion

    #region Spotify Methods
    public Uri SpotifySignInUri(string state)
    {
        try
        {
            return _spotifyManager.RequestUserAuthorizationUri(state);
        }
        catch (Exception)
        {
            throw;
        }
    }
    public async Task SpotifyRequestTokensAsync(string authCode, string originalStateCode, string returnedStateCode)
    {
        try
        {
            _tokens = await _spotifyManager.RequestAccessAndRefreshTokensAsync(authCode, originalStateCode, returnedStateCode);

            _timerManager.Stop();
            Votes.Clear();
            Clients.Clear();
        }
        catch (Exception)
        {
            throw;
        }
    }
    private async Task SpotifyRefreshAccessTokenAsync()
    {
        try
        {
            _tokens.AccessToken = await _spotifyManager.RefreshAccessTokenAsync(_tokens.RefreshToken);
        }
        catch (Exception)
        {
            throw;
        }
    }
    private async Task<Track> SpotifyGetCurrentlyPlayingAsync()
    {
        try
        {
            return await _spotifyManager.GetCurrentlyPlayingAsync() ?? new Track().MakeThisDummy();
        }
        catch (HttpRequestException ex)
        {
            try
            {
                await HandleHttpExceptionAsync(ex);

                return await _spotifyManager.GetCurrentlyPlayingAsync() ?? new Track().MakeThisDummy();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    public async Task<List<Track>> SpotifySearchTracksAsync(string searchFor)
    {
        try
        {
            return await _spotifyManager.SearchTracksAsync(searchFor);
        }
        catch (HttpRequestException ex)
        {
            try
            {
                await HandleHttpExceptionAsync(ex);

                return await _spotifyManager.SearchTracksAsync(searchFor);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    private async Task SpotifyAddToPlaybackQueueAsync(Track track)
    {
        try
        {
            await _spotifyManager.AddToPlaybackQueueAsync(track);
        }
        catch (HttpRequestException ex)
        {
            try
            {
                await HandleHttpExceptionAsync(ex);

                await _spotifyManager.AddToPlaybackQueueAsync(track);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    #endregion

    #region Voting Queue Methods
    public void EnqueueVote(Track votedTrack, Client client) => Votes.Enqueue(new Vote(votedTrack, client));
    public Vote? TryDequeueVote() => Votes.TryDequeue(out Vote? dequeuedVote) ? dequeuedVote : null;
    public void RemoveVote(Track track, Client client) => Votes = new(Votes.Where(v => !(v.VotedTrack.SongId == track.SongId && v.Client.Id == client.Id)));
    public Vote? TryPeekVotingQueue() => Votes.TryPeek(out Vote? voteOnTop) ? voteOnTop : null;
    private Vote MakeDummyVote() => new (new Track().MakeThisDummy(), new Client("0", "Spotify"));
    #endregion

    #region Timer Methods
    protected virtual void OnTimerUpdate() => TimerUpdateEvent?.Invoke(this, EventArgs.Empty);
    public void StartTimer(int seconds = 5)
    {
        if (_timerManager.HasStarted)
            StopTimer();

        _timerManager.TimeElapsed += OnTimerElapsedAsync;

        _timerManager.Start(seconds);

        _loopPeriodInMs = seconds * 1000;
    }

    public void StopTimer()
    {
        _timerManager.TimeElapsed -= OnTimerElapsedAsync;

        _timerManager.Stop();
    }

    private async void OnTimerElapsedAsync(object source, EventArgs args)
    {
        Track newestTrack = await SpotifyGetCurrentlyPlayingAsync();
        Vote? nextVote = TryPeekVotingQueue();

        if (newestTrack.SongId != CurrentlyPlayingVote.VotedTrack.SongId)
        {
            PreviouslyPlayedVote = CurrentlyPlayingVote;

            if (nextVote is not null && newestTrack.SongId == nextVote.VotedTrack.SongId)
            {
                TryDequeueVote();
                CurrentlyPlayingVote = nextVote;
            }
            else
                CurrentlyPlayingVote = new Vote(newestTrack, new Client("0", "Spotify"));
        }

        if (nextVote is not null && !nextVote.IsOnSpotifyQueue && (newestTrack.DurationMs - newestTrack.ProgressMs) < _loopPeriodInMs)
        {
            await SpotifyAddToPlaybackQueueAsync(nextVote.VotedTrack);
            nextVote.IsOnSpotifyQueue = true;
        }

        // TODO: else add a recommended track

        OnTimerUpdate();
    }
    #endregion

    #region Client Methods
    public void UpdateClient(Client clientUpdated) => Clients[Clients.FindIndex(c => c.Id == clientUpdated.Id)] = clientUpdated;    // TODO: change the client in the queue
    #endregion

    #region Other Methods
    private async Task HandleHttpExceptionAsync(HttpRequestException exception)
    {
        try
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
                await SpotifyRefreshAccessTokenAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion
}
