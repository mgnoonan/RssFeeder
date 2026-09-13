using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using RssFeeder.Models;
using System.Text.RegularExpressions;

var options = ParseOptions(args);
if (options.ShowHelp)
{
	PrintUsage();
	return;
}

if (!IsSafeIdentifier(options.CollectionName))
{
	Console.WriteLine($"Invalid collection name: '{options.CollectionName}'.");
	Console.WriteLine("Collection name may only contain letters, numbers, underscore, and dash.");
	return;
}

if (options.SelectedTypes.Count > 0)
{
		Console.WriteLine($"Filtering metrics to schema types: {string.Join(", ", options.SelectedTypes)}");
}

Console.WriteLine($"Connecting to RavenDB at {options.ServerUrl}");
Console.WriteLine($"Database: {options.DatabaseName}");
Console.WriteLine($"Collection: {options.CollectionName}");

using IDocumentStore store = new DocumentStore
{
	Urls = [options.ServerUrl],
	Database = options.DatabaseName
}.Initialize();

List<RssFeedItem> feedItems;
using (var session = store.OpenSession())
{
	var queryText = $"from {options.CollectionName}";
	if (options.Limit > 0)
	{
		queryText += $" limit {options.Limit}";
	}

	feedItems = session.Advanced.RawQuery<RssFeedItem>(queryText).ToList();
}

if (feedItems.Count == 0)
{
	Console.WriteLine("No documents were returned by the query.");
	return;
}

var metrics = BuildMetrics(feedItems, options.SelectedTypes);
Console.WriteLine(JsonConvert.SerializeObject(metrics, Formatting.Indented));

static JsonLdMetricsReport BuildMetrics(IReadOnlyList<RssFeedItem> feedItems, IReadOnlyCollection<string> selectedTypes)
{
	var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	var contextCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	var normalizedTypeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	var videoObjectSiteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	var videoObjectSiteNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	var articleLikeSiteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	var personSiteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	var organizationSiteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	var breadcrumbListSiteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	var webPageSiteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	var schemaMetrics = new SchemaSpecificMetrics();

	int docsWithJsonLd = 0;
	int totalJsonLdObjects = 0;
	int malformedObjects = 0;

	foreach (var item in feedItems)
	{
		if (item.JsonLdObjects is null || item.JsonLdObjects.Count == 0)
		{
			continue;
		}

		var documentHasMatchingJsonLd = false;

		foreach (var obj in item.JsonLdObjects)
		{
			if (obj is null)
			{
				continue;
			}

			var types = ReadValues(obj, "@type").ToList();
			if (types.Count == 0)
			{
				if (selectedTypes.Count == 0)
				{
					malformedObjects++;
					schemaMetrics.ObjectsWithoutType++;
				}
				continue;
			}

			var normalizedTypes = types
				.Select(NormalizeSchemaType)
				.Where(static t => !string.IsNullOrWhiteSpace(t))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (selectedTypes.Count > 0 && !normalizedTypes.Any(type => selectedTypes.Contains(type, StringComparer.OrdinalIgnoreCase)))
			{
				continue;
			}

			if (!documentHasMatchingJsonLd)
			{
				docsWithJsonLd++;
				documentHasMatchingJsonLd = true;
			}

			totalJsonLdObjects++;
			schemaMetrics.ObjectsWithType++;

			foreach (var contextValue in ReadValues(obj, "@context"))
			{
				Increment(contextCounts, contextValue);
			}

			foreach (var typeValue in types)
			{
				Increment(typeCounts, typeValue);
			}

			foreach (var typeValue in normalizedTypes)
			{
				Increment(normalizedTypeCounts, typeValue);
			}

			ApplySchemaSpecificMetrics(
				schemaMetrics,
				obj,
				normalizedTypes,
				item.SiteName,
				videoObjectSiteNames,
				videoObjectSiteNameCounts,
				articleLikeSiteNames,
				personSiteNames,
				organizationSiteNames,
				breadcrumbListSiteNames,
				webPageSiteNames);
		}
	}

	schemaMetrics.ArticleLikeCompleteness = BuildCompleteness(schemaMetrics.ArticleLikeObjects, schemaMetrics.ArticleLikeMissingHeadline, schemaMetrics.ArticleLikeMissingUrl);
	schemaMetrics.VideoObjectCompleteness = BuildCompleteness(schemaMetrics.VideoObjects, schemaMetrics.VideoObjectsMissingUrl, schemaMetrics.VideoObjectsMissingEmbedUrl);

	return new JsonLdMetricsReport
	{
		GeneratedAtUtc = DateTime.UtcNow,
		TotalDocuments = feedItems.Count,
		DocumentsWithJsonLd = docsWithJsonLd,
		TotalJsonLdObjects = totalJsonLdObjects,
		MalformedOrTypeMissingObjects = malformedObjects,
		AverageJsonLdObjectsPerDocument = totalJsonLdObjects / (double)feedItems.Count,
		TopTypes = typeCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Take(20).Select(p => new MetricCount(p.Key, p.Value)).ToList(),
		TopContexts = contextCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Take(20).Select(p => new MetricCount(p.Key, p.Value)).ToList(),
		TopNormalizedTypes = normalizedTypeCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Take(20).Select(p => new MetricCount(p.Key, p.Value)).ToList(),
		SelectedTypes = selectedTypes.ToList(),
		SchemaSpecific = schemaMetrics
	};
}

static void ApplySchemaSpecificMetrics(
	SchemaSpecificMetrics metrics,
	JObject obj,
	IReadOnlyCollection<string> normalizedTypes,
	string siteName,
	ISet<string> videoObjectSiteNames,
	Dictionary<string, int> videoObjectSiteNameCounts,
	ISet<string> articleLikeSiteNames,
	ISet<string> personSiteNames,
	ISet<string> organizationSiteNames,
	ISet<string> breadcrumbListSiteNames,
	ISet<string> webPageSiteNames)
{
	if (IsType(normalizedTypes, "Article") || IsType(normalizedTypes, "NewsArticle") || IsType(normalizedTypes, "BlogPosting"))
	{
		metrics.ArticleLikeObjects++;
		AddSiteName(articleLikeSiteNames, siteName);

		if (!HasAnyTextValue(obj, "headline", "name"))
		{
			metrics.ArticleLikeMissingHeadline++;
		}

		if (!HasAnyTextValue(obj, "url", "mainEntityOfPage"))
		{
			metrics.ArticleLikeMissingUrl++;
		}
	}

	if (IsType(normalizedTypes, "VideoObject"))
	{
		metrics.VideoObjects++;

		AddSiteName(videoObjectSiteNames, siteName);
		Increment(videoObjectSiteNameCounts, siteName);

		if (!HasAnyTextValue(obj, "url", "contentUrl"))
		{
			metrics.VideoObjectsMissingUrl++;
		}

		if (!HasAnyTextValue(obj, "embedUrl"))
		{
			metrics.VideoObjectsMissingEmbedUrl++;
		}
	}

	if (IsType(normalizedTypes, "Person"))
	{
		metrics.PersonObjects++;
		AddSiteName(personSiteNames, siteName);
	}

	if (IsType(normalizedTypes, "Organization"))
	{
		metrics.OrganizationObjects++;
		AddSiteName(organizationSiteNames, siteName);
	}

	if (IsType(normalizedTypes, "BreadcrumbList"))
	{
		metrics.BreadcrumbListObjects++;
		AddSiteName(breadcrumbListSiteNames, siteName);
	}

	if (IsType(normalizedTypes, "WebPage"))
	{
		metrics.WebPageObjects++;
		AddSiteName(webPageSiteNames, siteName);
	}

	metrics.ArticleLikeSiteNames = articleLikeSiteNames.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToList();
	metrics.VideoObjectSiteNames = videoObjectSiteNames.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToList();
	metrics.VideoObjectSiteNameCounts = videoObjectSiteNameCounts.OrderByDescending(static pair => pair.Value).ThenBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => new MetricCount(pair.Key, pair.Value)).ToList();
	metrics.PersonSiteNames = personSiteNames.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToList();
	metrics.OrganizationSiteNames = organizationSiteNames.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToList();
	metrics.BreadcrumbListSiteNames = breadcrumbListSiteNames.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToList();
	metrics.WebPageSiteNames = webPageSiteNames.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToList();
}

static void AddSiteName(ISet<string> siteNames, string siteName)
{
	if (!string.IsNullOrWhiteSpace(siteName))
	{
		siteNames.Add(siteName.Trim());
	}
}

static bool IsType(IReadOnlyCollection<string> types, string targetType)
{
	return types.Contains(targetType, StringComparer.OrdinalIgnoreCase);
}

static string NormalizeSchemaType(string typeValue)
{
	if (string.IsNullOrWhiteSpace(typeValue))
	{
		return string.Empty;
	}

	var trimmed = typeValue.Trim();
	var hashIndex = trimmed.LastIndexOf('#');
	var slashIndex = trimmed.LastIndexOf('/');
	var cut = Math.Max(hashIndex, slashIndex);

	if (cut >= 0 && cut + 1 < trimmed.Length)
	{
		return trimmed[(cut + 1)..];
	}

	return trimmed;
}

static bool HasAnyTextValue(JObject obj, params string[] propertyNames)
{
	foreach (var propertyName in propertyNames)
	{
		if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token)
			&& token is not null
			&& ExpandTokenToStrings(token).Any(static s => !string.IsNullOrWhiteSpace(s)))
		{
			return true;
		}
	}

	return false;
}

static CompletenessMetrics BuildCompleteness(int total, int missingPrimary, int missingSecondary)
{
	if (total <= 0)
	{
		return new CompletenessMetrics();
	}

	return new CompletenessMetrics
	{
		MissingPrimaryCount = missingPrimary,
		MissingSecondaryCount = missingSecondary,
		PrimaryCoverage = (total - missingPrimary) / (double)total,
		SecondaryCoverage = (total - missingSecondary) / (double)total
	};
}

static IEnumerable<string> ReadValues(JObject obj, string propertyName)
{
	if (!obj.TryGetValue(propertyName, out var token) || token is null)
	{
		yield break;
	}

	foreach (var value in ExpandTokenToStrings(token))
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			yield return value.Trim();
		}
	}
}

static IEnumerable<string> ExpandTokenToStrings(JToken token)
{
	if (token.Type == JTokenType.String)
	{
		var value = token.Value<string>();
		if (!string.IsNullOrWhiteSpace(value))
		{
			yield return value;
		}
		yield break;
	}

	if (token is JArray arr)
	{
		foreach (var child in arr)
		{
			foreach (var value in ExpandTokenToStrings(child))
			{
				yield return value;
			}
		}
		yield break;
	}

	if (token is JObject obj)
	{
		if (obj.TryGetValue("@id", out var idToken))
		{
			foreach (var value in ExpandTokenToStrings(idToken))
			{
				yield return value;
			}
		}

		if (obj.TryGetValue("@value", out var literalToken))
		{
			foreach (var value in ExpandTokenToStrings(literalToken))
			{
				yield return value;
			}
		}
	}
}

static void Increment(Dictionary<string, int> map, string key)
{
	if (map.TryGetValue(key, out var count))
	{
		map[key] = count + 1;
		return;
	}

	map[key] = 1;
}

static AppOptions ParseOptions(string[] args)
{
	var options = new AppOptions();

	for (var i = 0; i < args.Length; i++)
	{
		var arg = args[i];
		switch (arg)
		{
			case "-h":
			case "--help":
				options.ShowHelp = true;
				break;
			case "--url":
				options.ServerUrl = GetRequiredValue(args, ref i, arg);
				break;
			case "--database":
				options.DatabaseName = GetRequiredValue(args, ref i, arg);
				break;
			case "--collection":
				options.CollectionName = GetRequiredValue(args, ref i, arg);
				break;
			case "--limit":
				if (!int.TryParse(GetRequiredValue(args, ref i, arg), out var parsedLimit) || parsedLimit < 0)
				{
					throw new ArgumentException("--limit must be an integer >= 0");
				}
				options.Limit = parsedLimit;
				break;
			case "--types":
				options.AddSelectedTypes(GetRequiredValue(args, ref i, arg));
				break;
			default:
				throw new ArgumentException($"Unknown argument: {arg}");
		}
	}

	return options;
}

static string GetRequiredValue(string[] args, ref int index, string argName)
{
	if (index + 1 >= args.Length)
	{
		throw new ArgumentException($"Missing value for {argName}");
	}

	index++;
	return args[index];
}

static bool IsSafeIdentifier(string value)
{
	return Regex.IsMatch(value, "^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);
}

static void PrintUsage()
{
	Console.WriteLine("JSON-LD Metrics Analyzer (RavenDB)");
	Console.WriteLine();
	Console.WriteLine("Usage:");
	Console.WriteLine("  dotnet run -- [--url <raven-url>] [--database <db>] [--collection <collection>] [--limit <n>] [--types <type1,type2>]");
	Console.WriteLine();
	Console.WriteLine("Defaults:");
	Console.WriteLine("  --url        http://127.0.0.1:8080");
	Console.WriteLine("  --database   feed-items");
	Console.WriteLine("  --collection RssFeedItems");
	Console.WriteLine("  --limit      0 (no limit)");
	Console.WriteLine("  --types      all schema types (restricts all metrics to matching objects)");
}

public sealed class AppOptions
{
	public string ServerUrl { get; set; } = "http://127.0.0.1:8080";
	public string DatabaseName { get; set; } = "feed-items";
	public string CollectionName { get; set; } = "RssFeedItems";
	public int Limit { get; set; } = 0;
	public bool ShowHelp { get; set; }
	public List<string> SelectedTypes { get; } = [];

	public void AddSelectedTypes(string value)
	{
		foreach (var typeName in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (string.IsNullOrWhiteSpace(typeName))
			{
				continue;
			}

			if (!Regex.IsMatch(typeName, "^[A-Za-z0-9_.:-]+$", RegexOptions.CultureInvariant))
			{
				throw new ArgumentException($"Invalid schema type: {typeName}");
			}

			if (!SelectedTypes.Contains(typeName, StringComparer.OrdinalIgnoreCase))
			{
				SelectedTypes.Add(typeName);
			}
		}
	}
}

public sealed class JsonLdMetricsReport
{
	public DateTime GeneratedAtUtc { get; set; }
	public int TotalDocuments { get; set; }
	public int DocumentsWithJsonLd { get; set; }
	public int TotalJsonLdObjects { get; set; }
	public int MalformedOrTypeMissingObjects { get; set; }
	public double AverageJsonLdObjectsPerDocument { get; set; }
	public List<MetricCount> TopTypes { get; set; } = [];
	public List<MetricCount> TopContexts { get; set; } = [];
	public List<MetricCount> TopNormalizedTypes { get; set; } = [];
	public List<string> SelectedTypes { get; set; } = [];
	public SchemaSpecificMetrics SchemaSpecific { get; set; } = new();
}

public sealed record MetricCount(string Key, int Count);

public sealed class SchemaSpecificMetrics
{
	public int ObjectsWithType { get; set; }
	public int ObjectsWithoutType { get; set; }
	public int ArticleLikeObjects { get; set; }
	public int ArticleLikeMissingHeadline { get; set; }
	public int ArticleLikeMissingUrl { get; set; }
	public int VideoObjects { get; set; }
	public int VideoObjectsMissingUrl { get; set; }
	public int VideoObjectsMissingEmbedUrl { get; set; }
	public int PersonObjects { get; set; }
	public int OrganizationObjects { get; set; }
	public int BreadcrumbListObjects { get; set; }
	public int WebPageObjects { get; set; }
	public List<string> ArticleLikeSiteNames { get; set; } = [];
	public List<string> VideoObjectSiteNames { get; set; } = [];
	public List<MetricCount> VideoObjectSiteNameCounts { get; set; } = [];
	public List<string> PersonSiteNames { get; set; } = [];
	public List<string> OrganizationSiteNames { get; set; } = [];
	public List<string> BreadcrumbListSiteNames { get; set; } = [];
	public List<string> WebPageSiteNames { get; set; } = [];
	public CompletenessMetrics ArticleLikeCompleteness { get; set; } = new();
	public CompletenessMetrics VideoObjectCompleteness { get; set; } = new();
}

public sealed class CompletenessMetrics
{
	public int MissingPrimaryCount { get; set; }
	public int MissingSecondaryCount { get; set; }
	public double PrimaryCoverage { get; set; }
	public double SecondaryCoverage { get; set; }
}
