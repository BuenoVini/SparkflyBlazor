namespace RequestAPI;

internal class Program
{
    static async Task Main(string[] args)
    {
        Spotify.GetUserAuthorization();
    }
}
