using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.WriteLine("App starting");

// language=regex
string pattern = @"<tr>.*?javascript:D.'(?<token>.*?)'.*?popis.>\s+(?<popis>.*?)\s+</td>.*?datod.>\s*(?<date>.*?)\s+.*?zdroj.>\s*(?<zdroj>.*?)\s*</td>.*?</tr>";
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
    Token = m.Groups["token"].Value,
    Popis = m.Groups["popis"].Value,
    Date = m.Groups["date"].Value,
    Zdroj = m.Groups["zdroj"].Value,
}).Where(a => a.Date == DateTime.UtcNow.ToString("d.M.yyyy"));


foreach (var e in entries)
{
    Console.WriteLine(JsonSerializer.Serialize(e));
    string message = e.Popis.Replace('"', '\'');

    var payload = new
    {
        title = "Nový záznam",
        content = "Nový záznam",
        embeds = new List<object>(){new
        {
            author = new {
                name = e.Zdroj,
            },
            title = "Odkaz na www",
            description = message,
            color = 1127128,
            url = $"https://egov.opava-city.cz/Uredni_deska/DetailDokument.aspx?IdFile={e.Token}&Por=0",
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
