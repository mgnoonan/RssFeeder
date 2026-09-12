namespace RssFeeder.Console.Commands;

[Description("Report all database documents that contain video metadata", Name = "video-report")]
public class VideoReportCommand : OaktonCommand<VideoReportInput>
{
    private readonly IRepository _repository;
    private readonly ILogger _log;

    public VideoReportCommand(IRepository repository, ILogger log)
    {
        _repository = repository;
        _log = log;

        Usage("Scan feed documents for video metadata").Arguments(x => x.CollectionName);
    }

    public override bool Execute(VideoReportInput input)
    {
        string collectionName = string.IsNullOrWhiteSpace(input.CollectionName) ? "feed-items" : input.CollectionName;

        _log.Information("VIDEO_REPORT_START: collection='{collectionName}'", collectionName);

        var items = _repository.GetDocuments<RssFeedItem>(collectionName, "from RssFeedItems", null, false);
        int matchCount = 0;

        foreach (var item in items)
        {
            var videoOpenGraph = GetVideoOpenGraphAttributes(item.OpenGraphAttributes);
            var jsonLdVideoObjects = GetVideoJsonLdObjects(item.JsonLdObjects);

            if (videoOpenGraph.Count == 0 && jsonLdVideoObjects.Count == 0)
            {
                continue;
            }

            matchCount++;

            string siteName = item.SiteName;
            if (string.IsNullOrWhiteSpace(siteName))
            {
                siteName = item.OpenGraphAttributes.GetValueOrDefault("og:site_name") ?? item.HostName ?? string.Empty;
            }

            _log.Information("VIDEO_DOC: SiteName='{siteName}' UrlHash='{urlHash}' Url='{url}'", siteName, item.FeedAttributes?.UrlHash, item.FeedAttributes?.Url);

            if (videoOpenGraph.Count > 0)
            {
                _log.Information("OpenGraph video attributes: {videoOpenGraph}", JsonConvert.SerializeObject(videoOpenGraph));
            }
            else
            {
                _log.Information("OpenGraph video attributes: none");
            }

            if (jsonLdVideoObjects.Count > 0)
            {
                _log.Information("JsonLdObjects video entries: {jsonLdVideoObjects}", JsonConvert.SerializeObject(jsonLdVideoObjects, Formatting.None));
            }
            else
            {
                _log.Information("JsonLdObjects video entries: none");
            }
        }

        _log.Information("VIDEO_REPORT_END: scanned={scanned} matches={matches}", items.Count, matchCount);
        return true;
    }

    private static Dictionary<string, string> GetVideoOpenGraphAttributes(Dictionary<string, string> openGraphAttributes)
    {
        if (openGraphAttributes == null || openGraphAttributes.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return openGraphAttributes
            .Where(x => x.Key.StartsWith("og:video", StringComparison.OrdinalIgnoreCase)
                || x.Key.StartsWith("og:x:video", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private static List<JObject> GetVideoJsonLdObjects(List<JObject> jsonLdObjects)
    {
        var results = new List<JObject>();
        if (jsonLdObjects == null || jsonLdObjects.Count == 0)
        {
            return results;
        }

        foreach (var obj in jsonLdObjects)
        {
            foreach (var candidate in EnumerateJsonObjects(obj))
            {
                if (IsVideoObject(candidate))
                {
                    results.Add(candidate);
                }
            }
        }

        return results;
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
        if (typeToken == null)
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
}
