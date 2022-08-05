using Microsoft.AspNetCore.Components;
using System.Net;

namespace Sparkfly.Main.RequestAPI;

public class RequestAPIHandler
{
    public async Task HandleHttpRequestAsync(HttpRequestException exception, Spotify spotify, NavigationManager navManager)
    {
        try
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized)
                await spotify.RefreshAccessTokenAsync();
            else
                navManager.NavigateTo("/unhandled-error" + QueryString.Create("message", exception.Message));
        }
        catch (HttpRequestException)
        {
            navManager.NavigateTo("/unhandled-error" + QueryString.Create("message", exception.Message));
        }
    }
}
