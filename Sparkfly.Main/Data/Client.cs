namespace Sparkfly.Main.Data;

public class Client
{
    public string Id { get; private set; }
    public string? Name { get; set; }

    public Client(string id, string? name)  // NOTE: try to remove this constructor, may be unecessary
    {
        Id = id;
        Name = name;
    }
}
