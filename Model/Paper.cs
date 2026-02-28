namespace GrafAnalizi.Model;

public class Paper
{
    public string Id { get; set; } = "";
    public string Doi { get; set; } = "";
    public string Title { get; set; } = "";
    public int Year { get; set; }
    public List<string> Authors { get; set; } = new();
    public string Venue { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
    public List<string> ReferencedWorks { get; set; } = new();
    public int InJsonReferenceCount { get; set; }

    public string ShortId => Id.Contains("/") ? Id.Split('/').Last() : Id;

    public string AuthorsText => Authors.Count > 0 ? string.Join(", ", Authors) : "Bilinmiyor";

    public string ShortTitle => Title.Length > 80 ? Title[..77] + "..." : Title;
}
