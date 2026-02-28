namespace GrafAnalizi.Graph;

using GrafAnalizi.Model;

public class CitationGraph
{
    private readonly Dictionary<string, Paper> _nodes = new();
    
    private readonly Dictionary<string, HashSet<string>> _outEdges = new();
    
    private readonly Dictionary<string, HashSet<string>> _inEdges = new();

    private readonly HashSet<string> _visibleNodes = new();

    public void LoadFromPapers(List<Paper> papers)
    {
        _nodes.Clear();
        _outEdges.Clear();
        _inEdges.Clear();

        foreach (var paper in papers)
        {
            _nodes[paper.Id] = paper;
            _outEdges[paper.Id] = new HashSet<string>();
            _inEdges[paper.Id] = new HashSet<string>();
        }

        foreach (var paper in papers)
        {
            foreach (var refId in paper.ReferencedWorks)
            {
                if (_nodes.ContainsKey(refId))
                {
                    _outEdges[paper.Id].Add(refId);
                    _inEdges[refId].Add(paper.Id);
                }
            }
        }
    }

    public int NodeCount => _nodes.Count;

    public int EdgeCount
    {
        get
        {
            int count = 0;
            foreach (var edges in _outEdges.Values)
                count += edges.Count;
            return count;
        }
    }

    public int VisibleNodeCount => _visibleNodes.Count;

    public int VisibleEdgeCount
    {
        get
        {
            int count = 0;
            foreach (var nodeId in _visibleNodes)
            {
                if (_outEdges.TryGetValue(nodeId, out var edges))
                {
                    foreach (var targetId in edges)
                    {
                        if (_visibleNodes.Contains(targetId))
                            count++;
                    }
                }
            }
            return count;
        }
    }

    public void AddVisibleNode(string id)
    {
        if (_nodes.ContainsKey(id))
            _visibleNodes.Add(id);
    }

    public void AddVisibleNodes(IEnumerable<string> ids)
    {
        foreach (var id in ids)
            AddVisibleNode(id);
    }

    public void ClearVisibleNodes()
    {
        _visibleNodes.Clear();
    }

    public HashSet<string> GetVisibleNodes() => new(_visibleNodes);

    public IEnumerable<string> GetReferencedPapers(string id)
    {
        return _outEdges.TryGetValue(id, out var refs) ? refs : Enumerable.Empty<string>();
    }

    public IEnumerable<string> GetCitingPapers(string id)
    {
        return _inEdges.TryGetValue(id, out var citing) ? citing : Enumerable.Empty<string>();
    }

    public int GetVisibleInDegree(string id)
    {
        if (!_inEdges.TryGetValue(id, out var citing))
            return 0;
        
        return citing.Count(c => _visibleNodes.Contains(c));
    }

    public int GetVisibleOutDegree(string id)
    {
        if (!_outEdges.TryGetValue(id, out var refs))
            return 0;
        
        return refs.Count(r => _visibleNodes.Contains(r));
    }

    public int GetGlobalInDegree(string id)
    {
        return _inEdges.TryGetValue(id, out var citing) ? citing.Count : 0;
    }

    public int GetGlobalOutDegree(string id)
    {
        return _outEdges.TryGetValue(id, out var refs) ? refs.Count : 0;
    }

    public Paper? GetPaper(string id)
    {
        return _nodes.TryGetValue(id, out var paper) ? paper : null;
    }

    public Paper? FindByShortId(string shortId)
    {
        foreach (var paper in _nodes.Values)
        {
            if (paper.ShortId.Equals(shortId, StringComparison.OrdinalIgnoreCase))
                return paper;
        }
        return null;
    }

    public bool HasNode(string id) => _nodes.ContainsKey(id);

    public IEnumerable<Paper> GetAllPapers() => _nodes.Values;

    public IEnumerable<Paper> GetVisiblePapers()
    {
        foreach (var id in _visibleNodes)
        {
            if (_nodes.TryGetValue(id, out var paper))
                yield return paper;
        }
    }

    public (Paper? paper, int count) GetMostCitedVisible()
    {
        Paper? best = null;
        int maxCount = 0;

        foreach (var id in _visibleNodes)
        {
            int count = GetVisibleInDegree(id);
            if (count > maxCount)
            {
                maxCount = count;
                best = _nodes[id];
            }
        }

        return (best, maxCount);
    }

    public (Paper? paper, int count) GetMostReferencingVisible()
    {
        Paper? best = null;
        int maxCount = 0;

        foreach (var id in _visibleNodes)
        {
            int count = GetVisibleOutDegree(id);
            if (count > maxCount)
            {
                maxCount = count;
                best = _nodes[id];
            }
        }

        return (best, maxCount);
    }

    public (Paper? paper, int count) GetGlobalMaxCited()
    {
        Paper? best = null;
        int maxCount = 0;

        foreach (var id in _nodes.Keys)
        {
            int count = GetGlobalInDegree(id);
            if (count > maxCount)
            {
                maxCount = count;
                best = _nodes[id];
            }
        }

        return (best, maxCount);
    }

    public (Paper? paper, int count) GetGlobalMaxReferencing()
    {
        Paper? best = null;
        int maxCount = 0;

        foreach (var id in _nodes.Keys)
        {
            int count = GetGlobalOutDegree(id);
            if (count > maxCount)
            {
                maxCount = count;
                best = _nodes[id];
            }
        }

        return (best, maxCount);
    }
}
