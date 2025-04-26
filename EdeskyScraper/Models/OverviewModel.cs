namespace EdeskyScraper.Models;
public class OverviewModel
{
    public required string Token { get; set; }
    public required string Category { get; set; }
    public required string Date { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Source { get; set; }
    public string Url => $"https://egov.opava-city.cz/Uredni_deska/DetailDokument.aspx?IdFile={Token}&Por=0";
    public DetailModel? Detail { get; set; }
    public AttachmentModel[] Attachments { get; set; } = [];
}
