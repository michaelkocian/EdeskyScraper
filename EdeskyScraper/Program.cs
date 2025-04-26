using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.WriteLine("App starting");

// language=regex
string patternOverviewRegex = @"<tr>.*?kategorie.>\s*(?<category>.*?)\s*<.*?javascript:D.'(?<token>.*?)', '0'.;.>(?<title>.*?)</a>.*?popis.>\s*(?<desc>.*?)\s*<.*?datod.>\s*(?<date>.*?)\s*<.*?zdroj.>\s*(?<source>.*?)\s*<.*?</tr>";
// language=regex
string patternDetailRegex = @"parid=.(?<key>\w+).>(?<value>.*?)<";
// language=regex
string attachmentRegex = @"href=.(?<url>Dokument.*?).>(?<name>.*?)<.*?velikost.>.(?<size>.*?).<.*?soubor_poznamka_div.>(?<note>.*?)<";

string url = "https://egov.opava-city.cz/Uredni_deska/SeznamDokumentu.aspx";
string webhookUrl = "https://discord.com/api/webhooks/1365045974917058672/PsJhdkjYRAXzPanuNeFWqnR3MOWRhGziGTQAZWtc2iSRRGeq6jUymq63K_7mUi37QeQx";

using var http = new HttpClient();
string overviewContent = await http.GetStringAsync(url);
if (string.IsNullOrWhiteSpace(overviewContent))
{
    throw new Exception("unable to download webpage");
}

RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline;
MatchCollection overviewMatches = Regex.Matches(overviewContent, patternOverviewRegex, options);

var entries = overviewMatches.Select(m => new OverviewModel()
{
    Token = m.Groups["token"].Value.Replace('"', '\''),
    Category = m.Groups["category"].Value.Replace('"', '\''),
    Date = m.Groups["date"].Value.Replace('"', '\''),
    Title = m.Groups["title"].Value.Replace('"', '\''),
    Description = m.Groups["desc"].Value.Replace('"', '\''),
    Source = m.Groups["source"].Value.Replace('"', '\''),
}).ToArray();
var selectedEntries = entries.Where(a => a.Date == DateTime.UtcNow.AddDays(-1).ToString("d.M.yyyy")).ToArray();

//debug disable
selectedEntries = [];

foreach (var e in selectedEntries)
{
    string detailPageContent = await http.GetStringAsync(e.Url);
    if (string.IsNullOrWhiteSpace(detailPageContent))
    {
        throw new Exception("unable to download detail page");
    }

    Dictionary<string, string> detailDictionary = Regex.Matches(detailPageContent, patternDetailRegex, options)
    .Cast<Match>()
    .ToDictionary(
        m => m.Groups[1].Value.Replace('"', '\''),
        m => m.Groups[2].Value.Replace('"', '\'')
    );

    e.Detail = new DetailModel
    {
        Agenda = detailDictionary.GetValueOrDefault("agenda").Replace('"', '\''),
        Title = detailDictionary.GetValueOrDefault("nazev").Replace('"', '\''),
        Number = detailDictionary.GetValueOrDefault("cj").Replace('"', '\''),
        DateFrom = detailDictionary.GetValueOrDefault("zverejneno_od").Replace('"', '\''),
        DateTo = detailDictionary.GetValueOrDefault("zverejneno_do").Replace('"', '\''),
        Description = detailDictionary.GetValueOrDefault("anotace").Replace('"', '\''),
        Note = detailDictionary.GetValueOrDefault("poznamka").Replace('"', '\''),
        Source = detailDictionary.GetValueOrDefault("zdroj").Replace('"', '\''),
    };


    e.Attachments = Regex.Matches(detailPageContent, attachmentRegex, options)
    .Cast<Match>()
    .Select(m => new AttachmentModel()
    {
        UrlPathAndQuery = m.Groups["url"].Value,
        Name = m.Groups["name"].Value,
        Size = m.Groups["size"].Value,
        Note = m.Groups["note"].Value,
    }).ToArray();
}

foreach (var e in selectedEntries)
{
    Console.WriteLine(JsonSerializer.Serialize(e));
    var payload = new
    {
        title = $"{e.Category} / {e.Source}",
        content = $"{e.Category} / {e.Source}",
        embeds = new List<object>(){new
        {
            author = new {
                name = e.Title,
            },
            title = e.Category,
            description = e.Detail.Description,
            color = 1127128,
            url = e.Url,
            fields = new List<object>()
            {
                new {
                    name = "Date From",
                    value = e.Detail.DateFrom,
                    inline = true,
                },
                new {
                    name = "Date To",
                    value = e.Detail.DateTo,
                    inline = true,
                },
                new {
                    name = "Source",
                    value = e.Source,
                    inline = true,
                },
                new {
                    name = "Note",
                    value = e.Detail.Note,
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
    if (!discordresponse.IsSuccessStatusCode)
    {
        throw new Exception("discord declined the message");
    }
    var textresponse = await discordresponse.Content.ReadAsStringAsync();
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

File.WriteAllLines("lastrun.txt", entries.Select(e => e.Token));

Console.WriteLine("App finished successfully.");

public class OverviewModel
{
    public string Token { get; set; }
    public string Category { get; set; }
    public string Date { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Source { get; set; }
    public string Url => $"https://egov.opava-city.cz/Uredni_deska/DetailDokument.aspx?IdFile={Token}&Por=0";
    public DetailModel Detail { get; set; }
    public AttachmentModel[] Attachments { get; set; } = [];
}

public class DetailModel
{
    public string Agenda { get; set; }
    public string Title { get; set; }
    public string Number { get; set; }
    public string DateFrom { get; set; }
    public string DateTo { get; set; }
    public string Description { get; set; }
    public string Note { get; set; }
    public string Source { get; set; }
}

public class AttachmentModel
{
    public string Url => $"https://egov.opava-city.cz/Uredni_deska/{UrlPathAndQuery}";
    public string UrlPathAndQuery { get; set; }
    public string Size { get; set; }
    public string Name { get; set; }
    public string Note { get; set; }
}