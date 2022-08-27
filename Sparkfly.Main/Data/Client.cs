using System.ComponentModel.DataAnnotations;

namespace Sparkfly.Main.Data;

public class Client
{
    public string ClientId { get; private set; }

    //[Required(ErrorMessage = "Please inform your name.")]
    //[StringLength(15, ErrorMessage = "Name is too long.")]
    public string? ClientName { get; set; }

    public Client(string clientId, string? clientName)
    {
        ClientId = clientId;
        ClientName = clientName;
    }
}
