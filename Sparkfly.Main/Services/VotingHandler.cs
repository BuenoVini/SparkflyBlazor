using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;
using Sparkfly.Main.RequestAPI;

namespace Sparkfly.Main.Services;

public class VotingHandler
{
    private readonly ProtectedSessionStorage _currentSession;
    private readonly Spotify _spotify;

    public VotingHandler(ProtectedSessionStorage currentSession, Spotify spotify)
    {
        _currentSession = currentSession;
        _spotify = spotify;
    }

    public async Task<Queue<Vote>?> GetQueueAsync() => (await _currentSession.GetAsync<Queue<Vote>>("voting_queue")).Value;
    private async Task SetQueueAsync(Queue<Vote> votingQueue) => await _currentSession.SetAsync("voting_queue", votingQueue);
    public async Task<Vote?> PeekQueueAsync() => (await GetQueueAsync())?.Peek();

    public async Task EnqueueVoteAsync(Track trackVoted)
    {
        string? clientName = (await _currentSession.GetAsync<string>("client_name")).Value;
        Queue<Vote>? votingQueue = await GetQueueAsync();

        votingQueue ??= new Queue<Vote>();
        votingQueue.Enqueue(new Vote { TrackVoted = trackVoted, ClientName = clientName });

        await SetQueueAsync(votingQueue);
    }

    public async Task DequeueVoteAsync()
    {
        Queue<Vote>? votingQueue = await GetQueueAsync();
        Track? track = votingQueue?.Dequeue().TrackVoted;

        if (votingQueue is not null && track is not null)
        {
            await _spotify.AddToPlaybackQueueAsync(track);

            await SetQueueAsync(votingQueue);
        }
    }
}
