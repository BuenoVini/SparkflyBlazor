using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Sparkfly.Main.Data;

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

    public async Task EnqueueVoteAsync(Track votedTrack)
    {
        string? clientName = (await _currentSession.GetAsync<string>("client_name")).Value;
        Queue<Vote>? votingQueue = await GetQueueAsync();

        votingQueue ??= new Queue<Vote>();
        votingQueue.Enqueue(new Vote(votedTrack, clientName));

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
}
