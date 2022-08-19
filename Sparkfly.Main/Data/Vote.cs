namespace Sparkfly.Main.Data;

public class Vote
{
    public Track VotedTrack { get; set; }
    public string ClientName { get; set; }
    public bool IsOnSpotifyQueue { get; set; } = false;

    public Vote(Track votedTrack, string clientName)
    {
        VotedTrack = votedTrack;
        ClientName = clientName;
    }
}
