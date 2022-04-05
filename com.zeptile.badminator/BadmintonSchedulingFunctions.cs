using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using com.zeptile.badminator.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace com.zeptile.badminator;

public class BadmintonSchedulingFunctions
{
    private readonly List<BadmintonCredentials> _badmintonCredentials;
    private readonly string _discordWebhookUri;
    private readonly string _badmintonEndpoint;
    private readonly bool _headless;

    public BadmintonSchedulingFunctions()
    {
        _badmintonEndpoint = Environment.GetEnvironmentVariable("BADMINTON_ENDPOINT");
        _discordWebhookUri = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URI");
        var serializedCredentials = Environment.GetEnvironmentVariable("BADMINTON_CREDENTIALS");
        _badmintonCredentials = JsonConvert.DeserializeObject<List<BadmintonCredentials>>(serializedCredentials ?? string.Empty);
        _headless = bool.Parse(Environment.GetEnvironmentVariable("HEADLESS") ?? string.Empty);
    }
    
    [FunctionName("BadmintonAvailabilities")]
    public async Task<IActionResult> GetCurrentSchedulingAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        try
        {
            log.LogInformation("Getting Badminton Availabilities...");
        
            using var badmintonClient = new BadmintonClient(_badmintonEndpoint, _headless, _badmintonCredentials);

            await badmintonClient.Setup();
            var availabilities = await badmintonClient.GetBadmintonCourtAvailability();
        
            log.LogInformation("{} badminton court availabilities found", availabilities.Count);

            return availabilities.Count > 0
                ? new OkObjectResult(availabilities)
                : new NotFoundResult();
        }
        catch (Exception e)
        {
            log.LogError(e, "Unexpected error while trying to get badminton availabilities :: {}", e.Message);
            return new InternalServerErrorResult();
        }
        
    }
    
    [FunctionName("ReserveBadmintonCourt")]
    public async Task<IActionResult> ReserveBadmintonAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonConvert.DeserializeObject<ReserveCourtDto>(requestBody);

            if (dto is null || string.IsNullOrEmpty(dto.CourtCode)) return new BadRequestResult();
            
            log.LogInformation("Trying to reserve court [{}]", dto.CourtCode);

            using var badmintonClient = new BadmintonClient(_badmintonEndpoint, _headless, _badmintonCredentials);
            await badmintonClient.Setup();

            await badmintonClient.ReserveCourtAsync(_badmintonCredentials.First(), dto.CourtCode);
            
            return new OkResult();
        }
        catch (Exception e)
        {
            log.LogError(e, "Unexpected error while trying to reserve badminton court :: {}", e.Message);
            return new InternalServerErrorResult();
        }
        
    }
    
    [FunctionName("CancellationsCheck")]
    public async Task CheckForCancellationsAsync(
        [TimerTrigger("%CANCELLATION_TIMER_TRIGGER%")] TimerInfo myTimer, ILogger log)
    {
        try
        {
            log.LogInformation("CancellationsCheck Timer trigger function executed at: {}", DateTime.Now);
        
            using var badmintonClient = new BadmintonClient(_badmintonEndpoint, _headless, _badmintonCredentials);
    
            await badmintonClient.Setup();
            var availabilities = await badmintonClient.GetBadmintonCourtAvailability();
    
            var availableCourts = availabilities
                    .Where(x => x.Status == BadmintonAvailabilityStatus.Current)
                    .ToList();
        
            log.LogInformation("Found {} available badminton courts", availableCourts.Count);
    
            if (availableCourts.Count > 0)
            {
                log.LogInformation("Found available courts, sending notification to Discord");
                using var notificationClient = new DiscordNotificationClient(_discordWebhookUri);
                await notificationClient.SendAvailabilitiesAsync(availableCourts);
                return;
            }
            
            log.LogWarning("No badminton courts available");
        
        }
        catch (Exception e)
        {
            log.LogError(e, "Unexpected error while trying to get badminton availabilities :: {}", e.Message);
            throw;
        }
        
    }
    
    // [FunctionName("FridayScheduling")]
    // public async Task NewBadmintonFridaySchedulingAsync([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer, ILogger log)
    // {
    //     try
    //     {
    //         log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
    //     
    //         using var badmintonClient = new BadmintonClient(_badmintonEndpoint, _headless, _badmintonCredentials);
    //
    //         await badmintonClient.Setup();
    //         var availabilities = await badmintonClient.GetBadmintonCourtAvailability();
    //     
    //         log.LogInformation($"{availabilities.Count} badminton court availabilities found");
    //     }
    //     catch (Exception e)
    //     {
    //         log.LogError(e, $"Unexpected error while trying to get badminton availabilities :: {e.Message}");
    //         throw;
    //     }
    //     
    // }
    
}