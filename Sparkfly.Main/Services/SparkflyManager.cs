﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;
using Sparkfly.Main.Services.RequestApi;
using System.Net;

namespace Sparkfly.Main.Services;

public class SparkflyManager
{
    #region Attributes and Constructor
    private readonly SpotifyManager _spotify;
    private readonly VotingManager _votingHandler;

    private readonly NavigationManager _navigationManager;
    private readonly ProtectedSessionStorage _currentSession;

    public SparkflyManager(NavigationManager navigationManager, ProtectedSessionStorage protectedSession)
    {
        _navigationManager = navigationManager;
        _currentSession = protectedSession;

        _spotify = new (_navigationManager, _currentSession);
        _votingHandler = new (_currentSession);
    }
    #endregion

    #region Spotify Methods
    public async Task SpotifySignInAsync() => await _spotify.RequestUserAuthorizationAsync();
    public async Task SpotifyRequestTokensAsync(string code, string state) => await _spotify.RequestAccessAndRefreshTokensAsync(code, state);
    public async Task SpotifyRefreshTokenAsync() => await _spotify.RefreshAccessTokenAsync();
    public async Task<Track> SpotifyGetCurrentlyPlayingAsync() => await _spotify.GetCurrentlyPlayingAsync();
    public async Task<List<Track>> SpotifySearchTracksAsync(string searchFor) => await _spotify.SearchTracksAsync(searchFor);
    #endregion

    #region Queue Methods
    public async Task<Queue<Vote>?> GetVotingQueueAsync() => await _votingHandler.GetQueueAsync();
    public async Task<Vote?> PeekVotingQueueAsync() => await _votingHandler.PeekQueueAsync();
    public async Task EnqueueVoteAsync(Track track) => await _votingHandler.EnqueueVoteAsync(track);

    public async Task DequeueVoteAsync()
    {
        Track? nextTrack = await _votingHandler.DequeueVoteAsync();

        if (nextTrack is not null)
            await _spotify.AddToPlaybackQueueAsync(nextTrack);
    }
    #endregion

    #region API Methods
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
