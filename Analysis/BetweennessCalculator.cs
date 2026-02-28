namespace GrafAnalizi.Analysis;

using GrafAnalizi.Graph;

public class BetweennessCalculator
{
    private readonly CitationGraph _graph;

    public BetweennessCalculator(CitationGraph graph)
    {
        _graph = graph;
    }

    public Dictionary<string, double> Calculate()
    {
        var visibleNodes = _graph.GetVisibleNodes().ToList();
        var betweenness = new Dictionary<string, double>();

        foreach (var node in visibleNodes)
            betweenness[node] = 0.0;

        if (visibleNodes.Count < 2)
            return betweenness;

        var adjacency = BuildUndirectedAdjacency(visibleNodes);

        foreach (var source in visibleNodes)
        {
            var (predecessors, sigma, distance) = BFS(source, visibleNodes, adjacency);

            var delta = new Dictionary<string, double>();
            foreach (var node in visibleNodes)
                delta[node] = 0.0;

            var sortedByDistance = visibleNodes
                .Where(n => distance[n] >= 0 && n != source)
                .OrderByDescending(n => distance[n])
                .ToList();

            foreach (var w in sortedByDistance)
            {
                foreach (var v in predecessors[w])
                {
                    double contribution = (sigma[v] / (double)sigma[w]) * (1.0 + delta[w]);
                    delta[v] += contribution;
                }

                if (w != source)
                    betweenness[w] += delta[w];
            }
        }

        foreach (var node in visibleNodes)
            betweenness[node] /= 2.0;

        return betweenness;
    }

    private Dictionary<string, HashSet<string>> BuildUndirectedAdjacency(List<string> visibleNodes)
    {
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

        return adjacency;
    }

    private (Dictionary<string, List<string>> predecessors, Dictionary<string, int> sigma, Dictionary<string, int> distance)
        BFS(string source, List<string> nodes, Dictionary<string, HashSet<string>> adjacency)
    {
        var predecessors = new Dictionary<string, List<string>>();
        var sigma = new Dictionary<string, int>();
        var distance = new Dictionary<string, int>();

        foreach (var node in nodes)
        {
            predecessors[node] = new List<string>();
            sigma[node] = 0;
            distance[node] = -1;
        }

        sigma[source] = 1;
        distance[source] = 0;

        var queue = new Queue<string>();
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            var v = queue.Dequeue();

            foreach (var w in adjacency[v])
            {
                if (distance[w] < 0)
                {
                    distance[w] = distance[v] + 1;
                    queue.Enqueue(w);
                }

                if (distance[w] == distance[v] + 1)
                {
                    sigma[w] += sigma[v];
                    predecessors[w].Add(v);
                }
            }
        }

        return (predecessors, sigma, distance);
    }
}
