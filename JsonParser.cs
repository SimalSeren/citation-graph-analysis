namespace GrafAnalizi.Parser;

using GrafAnalizi.Model;

public class JsonParser
{
    public static List<Paper> ParseFile(string filePath)
    {
        string content = File.ReadAllText(filePath);
        return ParseJsonArray(content);
    }

    private static List<Paper> ParseJsonArray(string json)
    {
        var papers = new List<Paper>();
        json = json.Trim();

        if (!json.StartsWith("[") || !json.EndsWith("]"))
            throw new FormatException("Geçersiz JSON array formatı");

        json = json[1..^1].Trim();

        if (string.IsNullOrEmpty(json))
            return papers;

        int depth = 0;
        int objectStart = -1;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (c == '{')
            {
                if (depth == 0)
                    objectStart = i;
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && objectStart >= 0)
                {
                    string objectJson = json[objectStart..(i + 1)];
                    var paper = ParsePaperObject(objectJson);
                    if (paper != null)
                        papers.Add(paper);
                    objectStart = -1;
                }
            }
        }

        return papers;
    }

    private static Paper? ParsePaperObject(string json)
    {
        var paper = new Paper();

        json = json.Trim();
        if (!json.StartsWith("{") || !json.EndsWith("}"))
            return null;

        json = json[1..^1];

        paper.Id = ExtractStringValue(json, "id");
        paper.Doi = ExtractStringValue(json, "doi");
        paper.Title = ExtractStringValue(json, "title");
        paper.Year = ExtractIntValue(json, "year");
        paper.Venue = ExtractStringValue(json, "venue");
        paper.InJsonReferenceCount = ExtractIntValue(json, "in_json_reference_count");
        paper.Authors = ExtractStringArray(json, "authors");
        paper.Keywords = ExtractStringArray(json, "keywords");
        paper.ReferencedWorks = ExtractStringArray(json, "referenced_works");

        return paper;
    }

    private static string ExtractStringValue(string json, string key)
    {
        string pattern = $"\"{key}\"";
        int keyIndex = json.IndexOf(pattern);
        if (keyIndex < 0) return "";

        int colonIndex = json.IndexOf(':', keyIndex + pattern.Length);
        if (colonIndex < 0) return "";

        int valueStart = colonIndex + 1;
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            valueStart++;

        if (valueStart >= json.Length) return "";

        if (json.Substring(valueStart).StartsWith("null"))
            return "";

        if (json[valueStart] != '"')
            return "";

        int valueEnd = valueStart + 1;
        while (valueEnd < json.Length)
        {
            if (json[valueEnd] == '"' && json[valueEnd - 1] != '\\')
                break;
            valueEnd++;
        }

        if (valueEnd >= json.Length) return "";

        string value = json[(valueStart + 1)..valueEnd];
        
        value = value.Replace("\\\"", "\"");
        value = value.Replace("\\\\", "\\");
        value = value.Replace("\\n", "\n");
        value = value.Replace("\\r", "\r");
        value = value.Replace("\\t", "\t");

        return value;
    }

    private static int ExtractIntValue(string json, string key)
    {
        string pattern = $"\"{key}\"";
        int keyIndex = json.IndexOf(pattern);
        if (keyIndex < 0) return 0;

        int colonIndex = json.IndexOf(':', keyIndex + pattern.Length);
        if (colonIndex < 0) return 0;

        int valueStart = colonIndex + 1;
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            valueStart++;

        if (valueStart >= json.Length) return 0;

        int valueEnd = valueStart;
        while (valueEnd < json.Length && (char.IsDigit(json[valueEnd]) || json[valueEnd] == '-'))
            valueEnd++;

        if (valueEnd == valueStart) return 0;

        string numStr = json[valueStart..valueEnd];
        return int.TryParse(numStr, out int result) ? result : 0;
    }

    private static List<string> ExtractStringArray(string json, string key)
    {
        var result = new List<string>();

        string pattern = $"\"{key}\"";
        int keyIndex = json.IndexOf(pattern);
        if (keyIndex < 0) return result;

        int colonIndex = json.IndexOf(':', keyIndex + pattern.Length);
        if (colonIndex < 0) return result;

        int arrayStart = json.IndexOf('[', colonIndex);
        if (arrayStart < 0) return result;

        int depth = 1;
        int arrayEnd = arrayStart + 1;
        while (arrayEnd < json.Length && depth > 0)
        {
            if (json[arrayEnd] == '[') depth++;
            else if (json[arrayEnd] == ']') depth--;
            arrayEnd++;
        }

        if (depth != 0) return result;

        string arrayContent = json[(arrayStart + 1)..(arrayEnd - 1)].Trim();
        if (string.IsNullOrEmpty(arrayContent)) return result;

        bool inString = false;
        int stringStart = -1;
        
        for (int i = 0; i < arrayContent.Length; i++)
        {
            char c = arrayContent[i];

            if (c == '"' && (i == 0 || arrayContent[i - 1] != '\\'))
            {
                if (!inString)
                {
                    inString = true;
                    stringStart = i + 1;
                }
                else
                {
                    string value = arrayContent[stringStart..i];
                    value = value.Replace("\\\"", "\"");
                    value = value.Replace("\\\\", "\\");
                    result.Add(value);
                    inString = false;
                }
            }
        }

        return result;
    }
}
