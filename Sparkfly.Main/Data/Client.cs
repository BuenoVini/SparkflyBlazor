using System.ComponentModel.DataAnnotations;

namespace Sparkfly.Main.Data;

public class Client
{
    public string Id { get; private set; }
    public string? Name { get; set; }

    public Client(string id, string? name)
    {
        Id = id;
        Name = name;
    }
}
