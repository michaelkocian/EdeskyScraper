using EdeskyScraper;
using EdeskyScraper.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;


string[] ignored = [
    "přerušení dodávky elektrické energie",
    "možnost převzít písemnost",
    "Jamnice",
    "Háj ve Slezsku",
    "Sosnová",
    "Neplachovice",
    "Velké Heraltice",
    "Branka u Opavy",
    "Litultovice",
    "Jezdkovice",
    "Hněvošice",
    "Hrabyně",
    "Dolní Životice",
    "Loděnice",
    "Služovice",
    "Chlebičov",
    "Uhlířov",
    "Hradec nad Moravicí",
    "Jakartovice",
    "Stěbořice",
    "Mikolajice",
    ];

Console.WriteLine("App starting");
IConfigurationBuilder builder = new ConfigurationBuilder().AddEnvironmentVariables().AddUserSecrets<Program>();
IConfigurationRoot configuration = builder.Build();
string webhookUrl = configuration["DISCORD_WEBHOOK_URL"] ?? throw new Exception("discord webhook not passed into app.");


// language=regex
string patternOverviewRegex = @"<tr>.*?kategorie.>\s*(?<category>.*?)\s*<.*?javascript:D.'(?<token>.*?)', '(?<number>\d+)'.;.>(?<title>.*?)</a>.*?popis.>\s*(?<desc>.*?)\s*<.*?datod.>\s*(?<date>.*?)\s*<.*?zdroj.>\s*(?<source>.*?)\s*<.*?</tr>";
// language=regex
string patternDetailRegex = @"parid=.(?<key>\w+).>(?<value>.*?)<";
// language=regex
string attachmentRegex = @"href=.(?<url>Dokument.*?).>(?<name>.*?)<.*?velikost.>.(?<size>.*?).<.*?soubor_poznamka_div.>(?<note>.*?)<";
string filepath = "lastrun.txt";
string url = "https://egov.opava-city.cz/Uredni_deska/SeznamDokumentu.aspx";
RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline;

string[] lastRunTokens = File.Exists(filepath) ? await File.ReadAllLinesAsync(filepath) : [];

Console.WriteLine($"found {lastRunTokens.Length} token from last run.");

using var http = new HttpClient();
string overviewContent = await http.GetStringAsync(url);
if (string.IsNullOrWhiteSpace(overviewContent))
{
    throw new Exception("unable to download webpage");
}

MatchCollection overviewMatches = Regex.Matches(overviewContent, patternOverviewRegex, options);
OverviewModel[] entries = overviewMatches.GenerateEntries();
OverviewModel[] selectedEntries = entries.Where(e => !lastRunTokens.Contains(e.Token)).Reverse().ToArray();

foreach (var e in selectedEntries)
{
    string detailPageContent = await http.GetStringAsync(e.Url);
    if (string.IsNullOrWhiteSpace(detailPageContent))
    {
        throw new Exception("unable to download detail page");
    }
    MatchCollection detailMatches = Regex.Matches(detailPageContent, patternDetailRegex, options);
    e.Detail = detailMatches.GenerateDetail();

    MatchCollection attachmentMatches = Regex.Matches(detailPageContent, attachmentRegex, options);
    e.Attachments = attachmentMatches.GenerateAttachments();
}

int messageColor = Colors.GetTimeBasedColor(DateTime.UtcNow);
foreach (var e in selectedEntries)
{
    if (e.Detail?.Description?.Contains("Opav") == false 
        && ignored.Any(ign => e.Detail?.Description?.Contains(ign) == true))
    {
        continue;
    }

    Console.WriteLine($"SENDING: {e.Token}");
    var payload = new
    {
        username = e.Source,
        title = $"{e.Source} \n {e.Category}",
        //content = $"{e.Source} \n{e.Description}",
        embeds = new List<object>(){new
        {
            author = new {
                name = e.Title,
            },
            title = e.Category,
            description = e.Detail?.Description ?? "",
            color = messageColor,
            url = e.Url,
            fields = new List<object>()
            {
                new {
                    name = "Date From",
                    value = e.Detail?.DateFrom ?? "",
                    inline = true,
                },
                new {
                    name = "Date To",
                    value = e.Detail?.DateTo ?? "",
                    inline = true,
                },
                new {
                    name = "Source",
                    value = e.Source,
                    inline = true,
                },
                new {
                    name = "Note",
                    value = e.Detail?.Note ?? "",
                    inline = true,
                },
                new {
                    name = "Number",
                    value = e.Detail?.Number ?? "",
                    inline = true,
                },
            }.Concat(e.Attachments.Select(a => new {
                name = a.Note,
                value = $"[{a.Name}]({a.Url}) ({a.Size})",
                inline = true,
            }))
        } }
    };
    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var discordresponse = await http.PostAsync(webhookUrl, content);
    var textresponse = await discordresponse.Content.ReadAsStringAsync();
    if (!discordresponse.IsSuccessStatusCode)
    {
        throw new Exception("discord declined the message: " + textresponse);
    }
    await Task.Delay(1000);
}

int expectedCount = Regex.Matches(overviewContent, "</tr>").Count - 2;
int actualCount = entries.Length;

if (expectedCount != actualCount)
{
    Console.WriteLine("response: ");
    Console.WriteLine(overviewContent);
    throw new Exception($"Some messages was not extracted. actualCount: {actualCount}, expectedCount: {expectedCount}");
}

File.WriteAllLines(filepath, entries.Select(e => e.Token));

Console.WriteLine("App finished successfully.");
