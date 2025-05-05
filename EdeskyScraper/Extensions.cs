using EdeskyScraper.Models;
using System.Text.RegularExpressions;

namespace EdeskyScraper;
public static class Extensions
{
    public static OverviewModel[] GenerateEntries(this MatchCollection matches)
    {
        return matches.Select(m => new OverviewModel()
        {
            Token = m.Groups["token"].Value.Replace('"', '\''),
            Number = m.Groups["number"].Value.Replace('"', '\''),
            Category = m.Groups["category"].Value.Replace('"', '\''),
            Date = m.Groups["date"].Value.Replace('"', '\''),
            Title = m.Groups["title"].Value.Replace('"', '\''),
            Description = m.Groups["desc"].Value.Replace('"', '\''),
            Source = m.Groups["source"].Value.Replace('"', '\''),
        }).ToArray();
    }

    public static DetailModel GenerateDetail(this MatchCollection detailMatches)
    {
        Dictionary<string, string> detailDictionary = detailMatches
        .Cast<Match>()
        .ToDictionary(
            m => m.Groups[1].Value.Replace('"', '\''),
            m => m.Groups[2].Value.Replace('"', '\'')
        );

        return new DetailModel
        {
            Agenda = detailDictionary.GetValueOrDefault("agenda")?.Replace('"', '\'') ?? string.Empty,
            Title = detailDictionary.GetValueOrDefault("nazev")?.Replace('"', '\'') ?? string.Empty,
            Number = detailDictionary.GetValueOrDefault("cj")?.Replace('"', '\'') ?? string.Empty,
            DateFrom = detailDictionary.GetValueOrDefault("zverejneno_od")?.Replace('"', '\'') ?? string.Empty,
            DateTo = detailDictionary.GetValueOrDefault("zverejneno_do")?.Replace('"', '\'') ?? string.Empty,
            Description = detailDictionary.GetValueOrDefault("anotace")?.Replace('"', '\'') ?? string.Empty,
            Note = detailDictionary.GetValueOrDefault("poznamka")?.Replace('"', '\'') ?? string.Empty,
            Source = detailDictionary.GetValueOrDefault("zdroj")?.Replace('"', '\'') ?? string.Empty,
        };
    }

    public static AttachmentModel[] GenerateAttachments(this MatchCollection attachmentMatches)
    {
        return attachmentMatches
        .Select(m => new AttachmentModel()
        {
            UrlPathAndQuery = m.Groups["url"].Value,
            Name = m.Groups["name"].Value,
            Size = m.Groups["size"].Value,
            Note = m.Groups["note"].Value,
        }).ToArray();
    }
}
