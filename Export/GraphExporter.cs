namespace GrafAnalizi.Export;

using GrafAnalizi.Graph;
using GrafAnalizi.Model;
using System.Text;

public class GraphExporter
{
    private readonly CitationGraph _graph;

    public GraphExporter(CitationGraph graph)
    {
        _graph = graph;
    }

    public string ExportVisibleGraph(
        HashSet<string>? newlyAddedNodes = null,
        Dictionary<string, double>? betweennessScores = null,
        HashSet<string>? kCoreNodes = null,
        string? selectedNodeId = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        var visibleNodes = _graph.GetVisibleNodes();

        sb.AppendLine("  \"nodes\": [");
        bool firstNode = true;

        foreach (var id in visibleNodes)
        {
            var paper = _graph.GetPaper(id);
            if (paper == null) continue;

            if (!firstNode) sb.AppendLine(",");
            firstNode = false;

            string color = "#95A5A6";

            if (selectedNodeId != null && id == selectedNodeId)
            {
                color = "#E74C3C";
            }
            else if (kCoreNodes != null && kCoreNodes.Contains(id))
            {
                color = "#9B59B6";
            }
            else if (newlyAddedNodes != null && newlyAddedNodes.Contains(id))
            {
                color = "#F39C12";
            }
            else
            {
                color = "#95A5A6";
            }

            int size = 15;
            if (betweennessScores != null && betweennessScores.TryGetValue(id, out var bScore))
            {
                double maxScore = betweennessScores.Values.Max();
                if (maxScore > 0)
                    size = 15 + (int)(25 * (bScore / maxScore));
            }

            int inDegree = _graph.GetVisibleInDegree(id);
            
            string nodeLabel = $"[{inDegree}]\n{paper.ShortId}";

            string tooltip = BuildTooltip(paper, id);

            sb.Append($"    {{\"id\": \"{Escape(id)}\", ");
            sb.Append($"\"label\": \"{Escape(nodeLabel)}\", ");
            sb.Append($"\"title\": \"{tooltip}\", ");
            sb.Append($"\"color\": \"{color}\", ");
            sb.Append($"\"size\": {size}}}");
        }
        sb.AppendLine("\n  ],");

        sb.AppendLine("  \"edges\": [");
        bool firstEdge = true;
        var addedEdges = new HashSet<string>();

        foreach (var fromId in visibleNodes)
        {
            foreach (var toId in _graph.GetReferencedPapers(fromId))
            {
                if (!visibleNodes.Contains(toId)) continue;

                string edgeKey = $"{fromId}→{toId}";
                if (addedEdges.Contains(edgeKey)) continue;
                addedEdges.Add(edgeKey);

                if (!firstEdge) sb.AppendLine(",");
                firstEdge = false;

                string edgeColor = "#95A5A6";
                double width = 1;

                if (kCoreNodes != null && kCoreNodes.Contains(fromId) && kCoreNodes.Contains(toId))
                {
                    edgeColor = "#9B59B6";
                    width = 2;
                }
                else if (newlyAddedNodes != null && 
                    (newlyAddedNodes.Contains(fromId) || newlyAddedNodes.Contains(toId)))
                {
                    edgeColor = "#E67E22";
                    width = 1.5;
                }

                sb.Append($"    {{\"from\": \"{Escape(fromId)}\", ");
                sb.Append($"\"to\": \"{Escape(toId)}\", ");
                sb.Append($"\"color\": \"{edgeColor}\", ");
                sb.Append($"\"arrows\": \"to\", ");
                sb.Append($"\"width\": {width.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
            }
        }
        sb.AppendLine("\n  ]");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string BuildTooltip(Paper paper, string id)
    {
        int inDegree = _graph.GetVisibleInDegree(id);
        int outDegree = _graph.GetVisibleOutDegree(id);

        var tooltip = $"ID: {paper.ShortId}\nYazarlar: {paper.AuthorsText}\n{paper.ShortTitle}\nYıl: {paper.Year}\nAtıf Sayısı: {inDegree}\nReferans Sayısı: {outDegree}";
        return Escape(tooltip);
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    public string ExportStats()
    {
        var visibleNodes = _graph.GetVisibleNodes();
        
        int totalGiven = 0;
        int totalReceived = 0;
        
        foreach (var id in visibleNodes)
        {
            totalGiven += _graph.GetVisibleOutDegree(id);
            totalReceived += _graph.GetVisibleInDegree(id);
        }

        var (mostCited, citedCount) = _graph.GetMostCitedVisible();
        
        var (mostRef, refCount) = _graph.GetMostReferencingVisible();

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"totalNodes\": {_graph.NodeCount},");
        sb.AppendLine($"  \"totalEdges\": {_graph.EdgeCount},");
        sb.AppendLine($"  \"visibleNodes\": {_graph.VisibleNodeCount},");
        sb.AppendLine($"  \"visibleEdges\": {_graph.VisibleEdgeCount},");
        sb.AppendLine($"  \"totalGiven\": {totalGiven},");
        sb.AppendLine($"  \"totalReceived\": {totalReceived},");
        
        if (mostCited != null)
        {
            sb.AppendLine($"  \"maxCitedId\": \"{Escape(mostCited.ShortId)}\",");
            sb.AppendLine($"  \"maxCitedCount\": {citedCount},");
            sb.AppendLine($"  \"maxCitedTitle\": \"{Escape(mostCited.ShortTitle)}\",");
        }
        else
        {
            sb.AppendLine("  \"maxCitedId\": \"-\",");
            sb.AppendLine("  \"maxCitedCount\": 0,");
            sb.AppendLine("  \"maxCitedTitle\": \"\",");
        }
        
        if (mostRef != null)
        {
            sb.AppendLine($"  \"maxRefId\": \"{Escape(mostRef.ShortId)}\",");
            sb.AppendLine($"  \"maxRefCount\": {refCount},");
            sb.AppendLine($"  \"maxRefTitle\": \"{Escape(mostRef.ShortTitle)}\"");
        }
        else
        {
            sb.AppendLine("  \"maxRefId\": \"-\",");
            sb.AppendLine("  \"maxRefCount\": 0,");
            sb.AppendLine("  \"maxRefTitle\": \"\"");
        }
        
        sb.AppendLine("}");
        return sb.ToString();
    }
}
