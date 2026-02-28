namespace GrafAnalizi.Analysis;

using GrafAnalizi.Graph;

public class KCoreCalculator
{
    private readonly CitationGraph _graph;

    public KCoreCalculator(CitationGraph graph)
    {
        _graph = graph;
    }

    public KCoreResult Calculate(int k)
    {
        var result = new KCoreResult { K = k };

        var visibleNodes = _graph.GetVisibleNodes().ToList();
        
        if (visibleNodes.Count == 0 || k < 1)
            return result;

        var adjacency = new Dictionary<string, HashSet<string>>();
        var visibleSet = new HashSet<string>(visibleNodes);

        foreach (var node in visibleNodes)
            adjacency[node] = new HashSet<string>();

        foreach (var node in visibleNodes)
        {
            foreach (var target in _graph.GetReferencedPapers(node))
            {
                if (visibleSet.Contains(target))
                {
                    adjacency[node].Add(target);
                    adjacency[target].Add(node);
                }
            }
        }

        var remaining = new HashSet<string>(visibleNodes);
        bool changed = true;

        while (changed)
        {
            changed = false;
            var toRemove = new List<string>();

            foreach (var node in remaining)
            {
                int degree = adjacency[node].Count(neighbor => remaining.Contains(neighbor));

                if (degree < k)
                {
                    toRemove.Add(node);
                    changed = true;
                }
            }

            foreach (var node in toRemove)
                remaining.Remove(node);
        }

        result.CoreNodes = remaining.ToList();

        var addedEdges = new HashSet<string>();
        foreach (var node in remaining)
        {
            foreach (var neighbor in adjacency[node])
            {
                if (remaining.Contains(neighbor))
                {
                    string edgeKey = string.Compare(node, neighbor) < 0 
                        ? $"{node}|{neighbor}" 
                        : $"{neighbor}|{node}";
                    
                    if (!addedEdges.Contains(edgeKey))
                    {
                        addedEdges.Add(edgeKey);
                        result.CoreEdges.Add((node, neighbor));
                    }
                }
            }
        }

        return result;
    }
}

public class KCoreResult
{
    public int K { get; set; }
    public List<string> CoreNodes { get; set; } = new();
    public List<(string from, string to)> CoreEdges { get; set; } = new();

    public int NodeCount => CoreNodes.Count;
    public int EdgeCount => CoreEdges.Count;
}
