using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using com.zeptile.badminator.Constants;
using com.zeptile.badminator.Models;
using PuppeteerSharp;

namespace com.zeptile.badminator;

public class BadmintonClient : IDisposable
{
    private readonly string _endpoint;
    private readonly bool _headless;
    private readonly List<BadmintonCredentials> _credentialsList;

    private Browser _browser;
    
    public BadmintonClient(string endpoint, bool headless, List<BadmintonCredentials> credentialsList)
    {
        _endpoint = endpoint;
        _headless = headless;
        _credentialsList = credentialsList;
    }

    public async Task Setup()
    {
        var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions()
        {
            Path = Path.GetTempPath()
        });
            
        await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = _headless,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" },
            ExecutablePath = browserFetcher.RevisionInfo(BrowserFetcher.DefaultChromiumRevision).ExecutablePath
        });
    }

    public async Task<List<BadmintonAvailability>> GetBadmintonCourtAvailability()
    {
        await using var page = await _browser.NewPageAsync();
        await page.GoToAsync(GetSearchUrl("badminton"));
        await Login(page, _credentialsList.First());
        
        await page.WaitForSelectorAsync("#u5200_tableTableActivitySearch>tbody>tr");

        var results =
            await page.EvaluateFunctionAsync<List<BadmintonAvailability>>(InjectionScripts
                .GetAllBadmintonAvailabilityForPage);

        await page.CloseAsync();
        return results.ToList();
    }
    
    public async Task ReserveCourtAsync(BadmintonCredentials credentials, string availabilityCode)
    {
        await using var page = await _browser.NewPageAsync();
        await page.GoToAsync(GetSearchUrl(availabilityCode));
        await Login(page, credentials);

        try
        {
            
            await page.WaitForSelectorAsync("#u3600_btnSelect0", new WaitForSelectorOptions
            {
                Timeout = 5000
            });

        }
        catch (WaitTaskTimeoutException)
        {
            var results =
                await page.EvaluateFunctionAsync<List<BadmintonAvailability>>(InjectionScripts
                    .GetAllBadmintonAvailabilityForPage);

            var selectedAvailability = results.First(x => x.Code == availabilityCode);
            if (selectedAvailability.Status != BadmintonAvailabilityStatus.Current)
                throw new Exception("Court is no longer available");

            // Click on the Register button in the list of available courts
            await page.ClickAsync("#u5200_btnButtonRegister0");
            await page.WaitForSelectorAsync("#u3600_btnSelect0");
        }
        
        await page.ClickAsync("#u3600_btnSelect0");
        
        // Hard timeouts because Loisir website has a very slim window where the button cannot be clicked
        // Which causes Pupperteer to think it has successfully clicked on the button when it didn't
        
        // Click on Cart section finished button
        await page.WaitForSelectorAsync("#u3600_btnCheckout0");
        await page.WaitForTimeoutAsync(1000);
        await page.ClickAsync("#u3600_btnCheckout0");
        
        // Click on the Confirm button
        await page.WaitForSelectorAsync("#u3600_btnCartShoppingCompleteStep");
        await page.WaitForTimeoutAsync(1000);
        await page.ClickAsync("#u3600_btnCartShoppingCompleteStep");
        
        // Accept Terms & services
        await page.WaitForSelectorAsync("#u3600_chkElectronicPaymentCondition");
        await page.WaitForTimeoutAsync(1000);
        await page.ClickAsync("#u3600_chkElectronicPaymentCondition");
        
        // Click on the Confirm button
        await page.WaitForSelectorAsync("#u3600_btnCartPaymentCompleteStep");
        await page.WaitForTimeoutAsync(1000);
        await page.ClickAsync("#u3600_btnCartPaymentCompleteStep");
    }
    
    private async Task Login(Page page, BadmintonCredentials credentials)
    {
        try
        {
            await page.WaitForSelectorAsync("#formpr", new WaitForSelectorOptions
            {
                Timeout = 2000
            });
        } 
        catch (WaitTaskTimeoutException)
        {
            // Sometimes Loisir page does not redirect you to the login page
            // if it doesn't, click on login button manually
            await page.ClickAsync("#u2000_btnSignIn");
            await page.WaitForSelectorAsync("#formpr");
        }

        await page.TypeAsync("input[name=Email]#Email", credentials.Email);
        await page.TypeAsync("input[name=Password]#Password", credentials.Password);
        await page.ClickAsync("input[type=submit].bt-connexion");

        await page.WaitForNavigationAsync(new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
        });
        
    }


    private string GetSearchUrl(string searchString)
    {
        return _endpoint.Replace("SEARCH_PATTERN", searchString);
    }

    public async void Dispose()
    {
        await _browser.CloseAsync();
    }
}