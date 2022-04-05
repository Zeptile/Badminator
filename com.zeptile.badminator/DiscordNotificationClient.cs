using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.zeptile.badminator.Models;
using Discord;
using Discord.Webhook;

namespace com.zeptile.badminator;

public class DiscordNotificationClient : IDisposable
{
    private readonly DiscordWebhookClient _client;
    
    public DiscordNotificationClient(string webhookUri)
    {
        _client = new DiscordWebhookClient(webhookUri);
    }

    public async Task SendAvailabilitiesAsync(List<BadmintonAvailability> availabilities)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Current Availabilities")
            .WithDescription("Found available Badminton courts in the following locations:")
            .WithColor(new Color(Color.Blue))
            .WithTimestamp(DateTime.Now);

        foreach (var availability in availabilities)
        {
            embedBuilder.AddField($"{availability.Site} [{availability.Code}]",
                $"{availability.StartDate:dd/MM/yyyy} :: {availability.Schedule}");
        }
        
        await _client.SendMessageAsync("", false, new[] { embedBuilder.Build() });
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}