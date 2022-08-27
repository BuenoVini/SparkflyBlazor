using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;
using System.Reflection.Metadata.Ecma335;

namespace Sparkfly.Main.Services;

public class VotingManager
{
    private readonly ProtectedSessionStorage _currentSession;

    public VotingManager(ProtectedSessionStorage currentSession)
    {
        _currentSession = currentSession;
    }

    private async Task SetQueueAsync(Queue<Vote> votingQueue) => await _currentSession.SetAsync("voting_queue", votingQueue);
    public async Task<Queue<Vote>?> GetQueueAsync() => (await _currentSession.GetAsync<Queue<Vote>>("voting_queue")).Value;

    public async Task EnqueueVoteAsync(Track votedTrack, string clientId)
    {
        Queue<Vote>? votingQueue = await GetQueueAsync() ?? new Queue<Vote>();

        votingQueue.Enqueue(new Vote(votedTrack, clientId));

        await SetQueueAsync(votingQueue);
    }

    public async Task<Track?> DequeueVoteAsync()
    {
        Queue<Vote>? votingQueue = await GetQueueAsync();
        Track? track = votingQueue?.Dequeue().VotedTrack;

        if (votingQueue is not null && track is not null)
            await SetQueueAsync(votingQueue);

        return track;
    }

    public async Task<bool> RemoveVoteAsync(Track track, string clientId)
    {
        Queue<Vote>? votingQueue = await GetQueueAsync();

        if (votingQueue is null)
            return false;

        votingQueue = new (votingQueue.Where(x => !(x.VotedTrack.SongId == track.SongId && x.ClientId == clientId)));

        await SetQueueAsync(votingQueue);

        return true;
    }
}
