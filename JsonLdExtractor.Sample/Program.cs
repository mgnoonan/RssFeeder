using Newtonsoft.Json;
using JsonLdExtractor.Sample;
using System.Text.RegularExpressions;

if (args.Length == 0)
{
	Console.WriteLine("Usage: dotnet run -- <url>");
	return;
}

if (!Uri.TryCreate(args[0], UriKind.Absolute, out var targetUrl))
{
	Console.WriteLine($"Invalid URL: {args[0]}");
	return;
}

using var httpClient = new HttpClient();
var html = await httpClient.GetStringAsync(targetUrl);
var jsonLdBlocks = ExtractJsonLdBlocks(html);

if (jsonLdBlocks.Count == 0)
{
	Console.WriteLine("No application/ld+json blocks were found.");
	return;
}

var extractor = new SemanticJsonLdExtractor();
var profile = new ExtractionProfile
{
	Name = "default",
	Fields =
	[
		new FieldSpec("name", "schema:name", "schema:headline"),
		new FieldSpec("url", "schema:url", "schema:mainEntityOfPage", "schema:embedUrl"),
        new FieldSpec("embedUrl", "schema:embedUrl"),
		new FieldSpec("width", "schema:width"),
		new FieldSpec("height", "schema:height")
	]
};

var extractedItems = new List<Dictionary<string, object?>>();
foreach (var block in jsonLdBlocks)
{
	try
	{
		extractedItems.AddRange(extractor.Extract(block, profile));
	}
	catch (Exception)
	{
		// Ignore malformed JSON-LD blocks and continue with remaining blocks.
	}
}

var videoObjects = extractedItems
	.Where(item => item.TryGetValue("@type", out var typeValue)
		&& typeValue is List<string> types
		&& types.Any(t => string.Equals(t, "VideoObject", StringComparison.OrdinalIgnoreCase)
			|| t.EndsWith("/VideoObject", StringComparison.OrdinalIgnoreCase)
			|| t.EndsWith("#VideoObject", StringComparison.OrdinalIgnoreCase)))
	.ToList();

Console.WriteLine(JsonConvert.SerializeObject(videoObjects, Formatting.Indented));

static List<string> ExtractJsonLdBlocks(string html)
{
	var blocks = new List<string>();
	var unique = new HashSet<string>(StringComparer.Ordinal);
	var pattern = "<script\\b(?<attrs>[^>]*)>(?<json>[\\s\\S]*?)</script>";
	var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3));

	foreach (Match match in matches)
	{
		var attrs = match.Groups["attrs"].Value;
		var scriptContent = match.Groups["json"].Value.Trim();
		if (string.IsNullOrWhiteSpace(scriptContent))
		{
			continue;
		}

		var isLdJsonType = Regex.IsMatch(attrs, "type\\s*=\\s*([\"'][^\"']*ld\\+json[^\"']*[\"']|[^\\s>]*ld\\+json[^\\s>]*)", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3));
		if (isLdJsonType)
		{
			if (unique.Add(scriptContent))
			{
				blocks.Add(scriptContent);
			}
			continue;
		}

		foreach (var candidate in ExtractEmbeddedJsonCandidates(scriptContent))
		{
			if (unique.Add(candidate))
			{
				blocks.Add(candidate);
			}
		}
	}

	return blocks;
}

static IEnumerable<string> ExtractEmbeddedJsonCandidates(string scriptContent)
{
	var candidates = new List<string>();
	var markerPattern = new Regex("\\\"@context\\\"|\\\"@graph\\\"", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(3));
	var markerMatches = markerPattern.Matches(scriptContent);

	foreach (Match markerMatch in markerMatches)
	{
		int markerIndex = markerMatch.Index;
		int start = FindOpeningJsonDelimiter(scriptContent, markerIndex);
		if (start < 0)
		{
			continue;
		}

		if (!TryReadBalancedJson(scriptContent, start, out int end))
		{
			continue;
		}

		var candidate = scriptContent[start..(end + 1)];
		try
		{
			var token = Newtonsoft.Json.Linq.JToken.Parse(candidate);
			if (LooksLikeJsonLd(token))
			{
				candidates.Add(candidate);
			}
		}
		catch
		{
			// Ignore invalid embedded JSON and continue scanning.
		}
	}

	return candidates;
}

static int FindOpeningJsonDelimiter(string content, int markerIndex)
{
	for (int i = markerIndex; i >= 0; i--)
	{
		char c = content[i];
		if (c == '{' || c == '[')
		{
			return i;
		}
	}

	return -1;
}

static bool TryReadBalancedJson(string content, int start, out int end)
{
	end = -1;
	if (start < 0 || start >= content.Length)
	{
		return false;
	}

	char root = content[start];
	if (root != '{' && root != '[')
	{
		return false;
	}

	int depth = 0;
	bool inString = false;
	char stringQuote = '\0';
	bool escaped = false;

	for (int i = start; i < content.Length; i++)
	{
		char c = content[i];

		if (inString)
		{
			if (escaped)
			{
				escaped = false;
				continue;
			}

			if (c == '\\')
			{
				escaped = true;
				continue;
			}

			if (c == stringQuote)
			{
				inString = false;
			}

			continue;
		}

		if (c == '\"' || c == '\'')
		{
			inString = true;
			stringQuote = c;
			continue;
		}

		if (c == '{' || c == '[')
		{
			depth++;
			continue;
		}

		if (c == '}' || c == ']')
		{
			depth--;
			if (depth == 0)
			{
				end = i;
				return true;
			}
		}
	}

	return false;
}

static bool LooksLikeJsonLd(Newtonsoft.Json.Linq.JToken token)
{
	if (token is Newtonsoft.Json.Linq.JObject obj)
	{
		if (obj.ContainsKey("@context") || obj.ContainsKey("@graph"))
		{
			return true;
		}

		foreach (var property in obj.Properties())
		{
			if (LooksLikeJsonLd(property.Value))
			{
				return true;
			}
		}
	}

	if (token is Newtonsoft.Json.Linq.JArray arr)
	{
		foreach (var child in arr)
		{
			if (LooksLikeJsonLd(child))
			{
				return true;
			}
		}
	}

	return false;
}
