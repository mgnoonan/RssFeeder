using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace RssFeeder.Console.Utility;

public sealed class SemanticJsonLdExtractor
{
    public List<JsonLdVideoObject> ExtractVideoObjectsFromHtml(string html)
    {
        var blocks = ExtractJsonLdBlocks(html);
        var results = new List<JsonLdVideoObject>();

        foreach (var block in blocks)
        {
            foreach (var obj in EnumerateJsonObjects(block))
            {
                if (!IsVideoObject(obj))
                {
                    continue;
                }

                var item = new JsonLdVideoObject
                {
                    name = ReadString(obj, "name"),
                    url = ReadString(obj, "url"),
                    embedUrl = ReadString(obj, "embedUrl"),
                    width = ReadInt(obj, "width"),
                    height = ReadInt(obj, "height")
                };

                results.Add(item);
            }
        }

        return results;
    }

    private static List<JToken> ExtractJsonLdBlocks(string html)
    {
        var blocks = new List<JToken>();
        var pattern = "<script\\b[^>]*type\\s*=\\s*[\"'][^\"']*ld\\+json[^\"']*[\"'][^>]*>(?<json>[\\s\\S]*?)</script>";
        var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var json = match.Groups["json"].Value.Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            try
            {
                blocks.Add(JToken.Parse(json));
            }
            catch
            {
                // Ignore malformed script blocks.
            }
        }

        return blocks;
    }

    private static IEnumerable<JObject> EnumerateJsonObjects(JToken token)
    {
        if (token is JObject obj)
        {
            yield return obj;
            foreach (var property in obj.Properties())
            {
                foreach (var nested in EnumerateJsonObjects(property.Value))
                {
                    yield return nested;
                }
            }
            yield break;
        }

        if (token is JArray arr)
        {
            foreach (var child in arr)
            {
                foreach (var nested in EnumerateJsonObjects(child))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool IsVideoObject(JObject obj)
    {
        var typeToken = obj["@type"] ?? obj["type"];
        if (typeToken is null)
        {
            return false;
        }

        if (typeToken.Type == JTokenType.String)
        {
            return IsVideoTypeName(typeToken.Value<string>());
        }

        if (typeToken is JArray typeArray)
        {
            foreach (var token in typeArray)
            {
                if (IsVideoTypeName(token.Value<string>()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsVideoTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        return string.Equals(typeName, "VideoObject", StringComparison.OrdinalIgnoreCase)
            || typeName.EndsWith("/VideoObject", StringComparison.OrdinalIgnoreCase)
            || typeName.EndsWith("#VideoObject", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadString(JObject obj, string fieldName)
    {
        if (obj[fieldName] is not JToken token)
        {
            return string.Empty;
        }

        if (token.Type == JTokenType.String)
        {
            return token.Value<string>() ?? string.Empty;
        }

        if (token is JObject valueObj)
        {
            if (valueObj.TryGetValue("@value", out var literal))
            {
                return literal.Value<string>() ?? string.Empty;
            }

            if (valueObj.TryGetValue("@id", out var iri))
            {
                return iri.Value<string>() ?? string.Empty;
            }
        }

        if (token is JArray arr && arr.Count > 0)
        {
            if (arr[0].Type == JTokenType.String)
            {
                return arr[0].Value<string>() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static int ReadInt(JObject obj, string fieldName)
    {
        if (obj[fieldName] is not JToken token)
        {
            return 0;
        }

        if (token.Type == JTokenType.Integer)
        {
            return token.Value<int>();
        }

        if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out int parsed))
        {
            return parsed;
        }

        if (token is JObject valueObj && valueObj.TryGetValue("@value", out var literal))
        {
            if (literal.Type == JTokenType.Integer)
            {
                return literal.Value<int>();
            }

            if (literal.Type == JTokenType.String && int.TryParse(literal.Value<string>(), out parsed))
            {
                return parsed;
            }
        }

        return 0;
    }
}

public sealed class JsonLdVideoObject
{
    public string name { get; set; }
    public string url { get; set; }
    public string embedUrl { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}