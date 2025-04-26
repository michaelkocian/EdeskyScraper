namespace EdeskyScraper.Models;
public class AttachmentModel
{
    public string Url => $"https://egov.opava-city.cz/Uredni_deska/{UrlPathAndQuery}";
    public required string UrlPathAndQuery { get; set; }
    public required string Size { get; set; }
    public required string Name { get; set; }
    public required string Note { get; set; }
}