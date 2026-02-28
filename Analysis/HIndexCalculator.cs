namespace GrafAnalizi.Analysis;

using GrafAnalizi.Graph;

public class HIndexCalculator
{
    private readonly CitationGraph _graph;

    public HIndexCalculator(CitationGraph graph)
    {
        _graph = graph;
    }

    public HIndexResult Calculate(string paperId)
    {
        var result = new HIndexResult
        {
            PaperId = paperId,
            Paper = _graph.GetPaper(paperId)
        };

        if (result.Paper == null)
            return result;

        var citingPapers = _graph.GetCitingPapers(paperId)
            .Where(id => _graph.GetVisibleNodes().Contains(id))
            .ToList();

        if (citingPapers.Count == 0)
        {
            citingPapers = _graph.GetCitingPapers(paperId).ToList();
        }

        if (citingPapers.Count == 0)
            return result;

        var citationCounts = new List<(string id, int citations)>();
        
        foreach (var citingId in citingPapers)
        {
            int citations = _graph.GetGlobalInDegree(citingId);
            citationCounts.Add((citingId, citations));
        }

        citationCounts = citationCounts.OrderByDescending(x => x.citations).ToList();

        int hIndex = 0;
        for (int h = 1; h <= citationCounts.Count; h++)
        {
            if (citationCounts[h - 1].citations >= h)
                hIndex = h;
            else
                break;
        }

        result.HIndex = hIndex;

        result.HCore = citationCounts
            .Take(hIndex)
            .Select(x => x.id)
            .ToList();

        result.HCoreCitations = citationCounts
            .Take(hIndex)
            .Select(x => x.citations)
            .ToList();

        if (result.HCoreCitations.Count > 0)
        {
            var sorted = result.HCoreCitations.OrderBy(x => x).ToList();
            int mid = sorted.Count / 2;
            
            if (sorted.Count % 2 == 0)
                result.HMedian = (sorted[mid - 1] + sorted[mid]) / 2.0;
            else
                result.HMedian = sorted[mid];
        }

        return result;
    }
}

public class HIndexResult
{
    public string PaperId { get; set; } = "";
    public GrafAnalizi.Model.Paper? Paper { get; set; }
    public int HIndex { get; set; }
    public List<string> HCore { get; set; } = new();
    public List<int> HCoreCitations { get; set; } = new();
    public double HMedian { get; set; }

    public int TotalCitations => HCore.Count > 0 ? HCore.Count : 0;
}
