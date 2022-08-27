namespace Sparkfly.Main.Data;

public class Vote
{
    public Track VotedTrack { get; set; }
    public string ClientId { get; set; }
    public bool IsOnSpotifyQueue { get; set; } = false;

    public Vote(Track votedTrack, string clientId)
    {
        VotedTrack = votedTrack;
        ClientId = clientId;
    }
}
