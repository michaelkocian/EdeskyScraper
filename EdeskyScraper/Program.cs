using System.Text;
using System.Text.RegularExpressions;

Console.WriteLine("Hello, World!");

// language=regex
string pattern = @"""
    <tr>.*?javascript:D.'(?<token>.*?)'.*?popis.>\s+(?<popis>.*?)\s+</td>.*?datod.>\s*(?<date>.*?)\s+.*?zdroj.>\s*(?<zdroj>.*?)\s*</td>.*?</tr>
    """;
string url = "https://egov.opava-city.cz/Uredni_deska/SeznamDokumentu.aspx";
string webhookUrl = "https://discord.com/api/webhooks/1365045974917058672/PsJhdkjYRAXzPanuNeFWqnR3MOWRhGziGTQAZWtc2iSRRGeq6jUymq63K_7mUi37QeQx";

using var http = new HttpClient();
string response = await http.GetStringAsync(url);

RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline;
MatchCollection matches = Regex.Matches(response, pattern, options);

Match s = matches.FirstOrDefault();

var my = s!.Groups["popis"];

Console.WriteLine(my.Value);

async Task SendDiscordNotification(string message)
{
    using var http = new HttpClient();
    var content = new StringContent($"{{\"content\":\"{EscapeForJson(message)}\"}}", Encoding.UTF8, "application/json");
    await http.PostAsync(webhookUrl, content);
}
static string EscapeForJson(string s) =>
    s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
