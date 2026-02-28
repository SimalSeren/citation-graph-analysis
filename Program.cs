using System.Net;
using System.Text;
using GrafAnalizi.Parser;
using GrafAnalizi.Graph;
using GrafAnalizi.Analysis;
using GrafAnalizi.Export;

namespace GrafAnalizi;

class Program
{
    private static CitationGraph _graph = new();
    private static GraphExporter _exporter = null!;
    private static HIndexCalculator _hIndexCalc = null!;
    private static BetweennessCalculator _betweennessCalc = null!;
    private static KCoreCalculator _kCoreCalc = null!;

    private static Dictionary<string, double>? _lastBetweenness;
    private static HashSet<string>? _lastKCore;
    private static HashSet<string> _newlyAddedNodes = new();
    private static string? _selectedNodeId = null;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║         GRAF ANALİZİ - Makale Atıf           ║");
        Console.WriteLine("║         Kocaeli Üniversitesi - Prolab        ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.WriteLine();

        string dataFile = FindDataFile(args);
        Console.WriteLine($"📁 Veri dosyası: {dataFile}");

        try
        {
            Console.WriteLine("📖 JSON dosyası okunuyor...");
            var papers = JsonParser.ParseFile(dataFile);
            Console.WriteLine($"✅ {papers.Count} makale yüklendi.");

            Console.WriteLine("🔗 Graf oluşturuluyor...");
            _graph.LoadFromPapers(papers);
            Console.WriteLine($"✅ Graf oluşturuldu: {_graph.NodeCount} düğüm, {_graph.EdgeCount} kenar");

            _exporter = new GraphExporter(_graph);
            _hIndexCalc = new HIndexCalculator(_graph);
            _betweennessCalc = new BetweennessCalculator(_graph);
            _kCoreCalc = new KCoreCalculator(_graph);

            await StartHttpServer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Hata: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static string FindDataFile(string[] args)
    {
        if (args.Length > 0 && File.Exists(args[0]))
            return args[0];

        if (File.Exists("data.json"))
            return "data.json";

        string forPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "GRAFANALİZİ_için", "data.json");
        if (File.Exists(forPath))
            return forPath;

        string rootPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data.json");
        if (File.Exists(rootPath))
            return rootPath;

        throw new FileNotFoundException("data.json dosyası bulunamadı!");
    }

    private static async Task StartHttpServer()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();

        Console.WriteLine();
        Console.WriteLine("🌐 HTTP Sunucu başlatıldı: http://localhost:8080");
        Console.WriteLine("📊 Tarayıcınızda açın ve graf analizine başlayın!");
        Console.WriteLine("🛑 Durdurmak için Ctrl+C");
        Console.WriteLine();

        while (true)
        {
            var context = await listener.GetContextAsync();
            _ = Task.Run(() => HandleRequest(context));
        }
    }

    private static void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            string path = request.Url?.AbsolutePath ?? "/";
            string responseText = "";
            string contentType = "application/json";

            switch (path)
            {
                case "/":
                    responseText = ServeIndexHtml();
                    contentType = "text/html; charset=utf-8";
                    break;

                case "/graph":
                    responseText = _exporter.ExportVisibleGraph(_newlyAddedNodes, _lastBetweenness, _lastKCore, _selectedNodeId);
                    break;

                case "/stats":
                    responseText = _exporter.ExportStats();
                    break;

                case "/global-stats":
                    HandleGlobalStats(out responseText);
                    break;

                case "/select":
                    HandleSelect(request, out responseText);
                    break;

                case "/h-index":
                    HandleHIndex(request, out responseText);
                    break;

                case "/betweenness":
                    HandleBetweenness(out responseText);
                    break;

                case "/k-core":
                    HandleKCore(request, out responseText);
                    break;

                case "/clear":
                    HandleClear(out responseText);
                    break;

                case "/search":
                    HandleSearch(request, out responseText);
                    break;

                default:
                    response.StatusCode = 404;
                    responseText = "{\"error\": \"Not Found\"}";
                    break;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseText);
            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HTTP Hata: {ex.Message}");
        }
    }

    private static void HandleSelect(HttpListenerRequest request, out string response)
    {
        string? id = request.QueryString["id"];
        
        if (string.IsNullOrEmpty(id))
        {
            response = "{\"error\": \"ID gerekli\"}";
            return;
        }

        var paper = _graph.GetPaper(id) ?? _graph.FindByShortId(id);
        
        if (paper == null)
        {
            response = "{\"error\": \"Makale bulunamadı\"}";
            return;
        }

        _newlyAddedNodes.Clear();
        _newlyAddedNodes.Add(paper.Id);
        _graph.AddVisibleNode(paper.Id);

        Console.WriteLine($"📌 Makale seçildi: {paper.ShortId}");

        response = $"{{\"success\": true, \"id\": \"{paper.Id}\", \"shortId\": \"{paper.ShortId}\"}}";
    }

    private static void HandleHIndex(HttpListenerRequest request, out string response)
    {
        string? id = request.QueryString["id"];
        
        if (string.IsNullOrEmpty(id))
        {
            response = "{\"error\": \"ID gerekli\"}";
            return;
        }

        var paper = _graph.GetPaper(id) ?? _graph.FindByShortId(id);
        
        if (paper == null)
        {
            response = "{\"error\": \"Makale bulunamadı\"}";
            return;
        }

        var result = _hIndexCalc.Calculate(paper.Id);

        _selectedNodeId = paper.Id;

        _newlyAddedNodes.Clear();
        foreach (var coreId in result.HCore)
        {
            if (!_graph.GetVisibleNodes().Contains(coreId))
            {
                _newlyAddedNodes.Add(coreId);
                _graph.AddVisibleNode(coreId);
            }
        }

        Console.WriteLine($"📊 H-Index ({paper.ShortId}): {result.HIndex}, H-Core: {result.HCore.Count} düğüm, H-Median: {result.HMedian:F1}");

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"paperId\": \"{paper.ShortId}\",");
        sb.AppendLine($"  \"paperTitle\": \"{Escape(paper.ShortTitle)}\",");
        sb.AppendLine($"  \"hIndex\": {result.HIndex},");
        sb.AppendLine($"  \"hMedian\": {result.HMedian.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"hCoreCount\": {result.HCore.Count},");
        sb.Append("  \"hCoreNodes\": [");
        for (int i = 0; i < result.HCore.Count; i++)
        {
            var corePaper = _graph.GetPaper(result.HCore[i]);
            if (i > 0) sb.Append(", ");
            sb.Append($"{{\"shortId\": \"{corePaper?.ShortId ?? ""}\", \"title\": \"{Escape(corePaper?.ShortTitle ?? "")}\", \"citations\": {result.HCoreCitations[i]}}}");
        }
        sb.AppendLine("],");
        sb.AppendLine($"  \"newNodesAdded\": {_newlyAddedNodes.Count}");
        sb.AppendLine("}");

        response = sb.ToString();
    }

    private static void HandleBetweenness(out string response)
    {
        _lastBetweenness = _betweennessCalc.Calculate();
        _lastKCore = null;

        Console.WriteLine($"📈 Betweenness Centrality hesaplandı: {_lastBetweenness.Count} düğüm");

        var top10 = _lastBetweenness
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"success\": true,");
        sb.Append("  \"top10\": [");
        for (int i = 0; i < top10.Count; i++)
        {
            var paper = _graph.GetPaper(top10[i].Key);
            if (i > 0) sb.Append(", ");
            sb.Append($"{{\"id\": \"{paper?.ShortId}\", \"score\": {top10[i].Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}}}");
        }
        sb.AppendLine("]");
        sb.AppendLine("}");

        response = sb.ToString();
    }

    private static void HandleKCore(HttpListenerRequest request, out string response)
    {
        string? kStr = request.QueryString["k"];
        
        if (string.IsNullOrEmpty(kStr) || !int.TryParse(kStr, out int k) || k < 1)
        {
            response = "{\"error\": \"Geçerli k değeri gerekli (k >= 1)\"}";
            return;
        }

        var result = _kCoreCalc.Calculate(k);
        _lastKCore = new HashSet<string>(result.CoreNodes);
        _lastBetweenness = null;

        Console.WriteLine($"🔵 K-Core (k={k}): {result.NodeCount} düğüm, {result.EdgeCount} kenar");

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"success\": true,");
        sb.AppendLine($"  \"k\": {k},");
        sb.AppendLine($"  \"nodeCount\": {result.NodeCount},");
        sb.AppendLine($"  \"edgeCount\": {result.EdgeCount},");
        sb.Append("  \"nodes\": [");
        for (int i = 0; i < result.CoreNodes.Count; i++)
        {
            var paper = _graph.GetPaper(result.CoreNodes[i]);
            if (i > 0) sb.Append(", ");
            sb.Append($"\"{paper?.ShortId}\"");
        }
        sb.AppendLine("]");
        sb.AppendLine("}");

        response = sb.ToString();
    }

    private static void HandleClear(out string response)
    {
        _graph.ClearVisibleNodes();
        _newlyAddedNodes.Clear();
        _lastBetweenness = null;
        _lastKCore = null;
        _selectedNodeId = null;

        Console.WriteLine("🗑️ Graf temizlendi");

        response = "{\"success\": true}";
    }

    private static void HandleGlobalStats(out string response)
    {
        var (maxCitedPaper, maxCitedCount) = _graph.GetGlobalMaxCited();
        var (maxRefPaper, maxRefCount) = _graph.GetGlobalMaxReferencing();

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"globalMaxCitedId\": \"{maxCitedPaper?.ShortId ?? ""}\",");
        sb.AppendLine($"  \"globalMaxCitedCount\": {maxCitedCount},");
        sb.AppendLine($"  \"globalMaxCitedTitle\": \"{Escape(maxCitedPaper?.ShortTitle ?? "")}\",");
        sb.AppendLine($"  \"globalMaxRefId\": \"{maxRefPaper?.ShortId ?? ""}\",");
        sb.AppendLine($"  \"globalMaxRefCount\": {maxRefCount},");
        sb.AppendLine($"  \"globalMaxRefTitle\": \"{Escape(maxRefPaper?.ShortTitle ?? "")}\"");
        sb.AppendLine("}");

        response = sb.ToString();
    }

    private static void HandleSearch(HttpListenerRequest request, out string response)
    {
        string? query = request.QueryString["q"];
        
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            response = "{\"results\": []}";
            return;
        }

        query = query.ToLowerInvariant();

        var results = _graph.GetAllPapers()
            .Where(p => 
                p.ShortId.ToLowerInvariant().Contains(query) ||
                p.Title.ToLowerInvariant().Contains(query) ||
                p.AuthorsText.ToLowerInvariant().Contains(query))
            .Take(20)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.Append("  \"results\": [");
        for (int i = 0; i < results.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append($"{{\"id\": \"{results[i].Id}\", \"shortId\": \"{results[i].ShortId}\", \"title\": \"{Escape(results[i].ShortTitle)}\", \"year\": {results[i].Year}}}");
        }
        sb.AppendLine("]");
        sb.AppendLine("}");

        response = sb.ToString();
    }

    private static string ServeIndexHtml()
    {
        string[] paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Web", "index.html"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Web", "index.html"),
            "Web/index.html"
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        return "<html><body><h1>index.html bulunamadı</h1></body></html>";
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
    }
}
