namespace Sparkfly.Main.Data;

public class Vote
{
    public Track VotedTrack { get; set; }
    public string? ClientName { get; set; } // TODO: remove nullable
    //public Client Client { get; set; }

    public Vote(Track votedTrack, string? clientName)
    {
        VotedTrack = votedTrack;
        ClientName = clientName;
    }
}
