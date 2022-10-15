namespace Sparkfly.Main.Data;

public class Client
{
    public string Id { get; private set; }
    public string Name { get; set; }
    public bool IsHost { get; set; }

    public Client(string id, string name, bool isHost = false)  // NOTE: try to remove this constructor, may be unecessary
    {
        Id = id;
        Name = name;
        IsHost = isHost;
    }
}
