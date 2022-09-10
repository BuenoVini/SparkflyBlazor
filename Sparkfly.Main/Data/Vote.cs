namespace Sparkfly.Main.Data;

public class Vote
{
    public Track VotedTrack { get; set; }
    public Client Client { get; set; }
    //public bool IsOnSpotifyQueue { get; set; } = false;

    public Vote(Track votedTrack, Client client)
    {
        VotedTrack = votedTrack;
        Client = client;
    }
}
