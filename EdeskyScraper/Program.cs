using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.WriteLine("App starting");

// language=regex
string pattern = @"<tr>.*?kategorie.>\s*(?<category>.*?)\s*<.*?javascript:D.'(?<token>.*?)', '0'.;.>(?<title>.*?)</a>.*?popis.>\s*(?<desc>.*?)\s*<.*?datod.>\s*(?<date>.*?)\s*<.*?zdroj.>\s*(?<source>.*?)\s*<.*?</tr>";
string url = "https://egov.opava-city.cz/Uredni_deska/SeznamDokumentu.aspx";
string webhookUrl = "https://discord.com/api/webhooks/1365045974917058672/PsJhdkjYRAXzPanuNeFWqnR3MOWRhGziGTQAZWtc2iSRRGeq6jUymq63K_7mUi37QeQx";

using var http = new HttpClient();
string response = await http.GetStringAsync(url);
if (string.IsNullOrWhiteSpace(response))
{
    throw new Exception("unable to download webpage");
}

RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline;
MatchCollection matches = Regex.Matches(response, pattern, options);

var entries = matches.Select(m => new
{
    Token = m.Groups["token"].Value.Replace('"', '\''),
    Category = m.Groups["category"].Value.Replace('"', '\''),
    Date = m.Groups["date"].Value.Replace('"', '\''),
    Title = m.Groups["title"].Value.Replace('"', '\''),
    Zdroj = m.Groups["zdroj"].Value.Replace('"', '\''),
    Description = m.Groups["desc"].Value.Replace('"', '\''),
    Source = m.Groups["source"].Value.Replace('"', '\''),
}).ToArray();
var selectedEntries = entries.Where(a => a.Date == DateTime.UtcNow.ToString("d.M.yyyy")).ToArray();

foreach (var e in selectedEntries)
{
    Console.WriteLine(JsonSerializer.Serialize(e));

    var payload = new
    {
        title = e.Date,
        content = e.Date,
        embeds = new List<object>(){new
        {
            author = new {
                name = e.Title,
            },
            title = e.Category,
            description = e.Description,
            color = 1127128,
            url = $"https://egov.opava-city.cz/Uredni_deska/DetailDokument.aspx?IdFile={e.Token}&Por=0",
            fields = new List<object>()
            {
                new {
                    name = "Date",
                    value = e.Date,
                    inline = true,
                },
                new {
                    name = "Source",
                    value = e.Source,
                    inline = true,
                },
            }
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

int expectedCount = Regex.Matches(response, "</tr>").Count - 2;
int actualCount = entries.Length;

if (expectedCount != actualCount)
{
    Console.WriteLine("response: ");
    Console.WriteLine(response);
    throw new Exception($"Some messages was not extracted. actualCount: {actualCount}, expectedCount: {expectedCount}");
}

Console.WriteLine("App finished successfully.");