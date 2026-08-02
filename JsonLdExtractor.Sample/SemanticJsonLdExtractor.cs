using JsonLD.Core;
using Newtonsoft.Json.Linq;

public sealed class SemanticJsonLdExtractor
{
	private static readonly Dictionary<string, string[]> PrefixMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["schema:name"] = ["http://schema.org/name", "https://schema.org/name"],
		["schema:headline"] = ["http://schema.org/headline", "https://schema.org/headline"],
		["schema:url"] = ["http://schema.org/url", "https://schema.org/url"],
		["schema:mainEntityOfPage"] = ["http://schema.org/mainEntityOfPage", "https://schema.org/mainEntityOfPage"],
		["schema:embedUrl"] = ["http://schema.org/embedUrl", "https://schema.org/embedUrl"],
		["schema:width"] = ["http://schema.org/width", "https://schema.org/width"],
		["schema:height"] = ["http://schema.org/height", "https://schema.org/height"]
	};

	public IReadOnlyList<Dictionary<string, object?>> Extract(string json, ExtractionProfile profile)
	{
		var nodes = ExpandToNodes(json);
		var output = new List<Dictionary<string, object?>>();

		foreach (var nodeToken in nodes)
		{
			if (nodeToken is not JObject node)
			{
				continue;
			}

			var item = new Dictionary<string, object?>
			{
				["@type"] = ReadTypes(node)
			};

			foreach (var field in profile.Fields)
			{
				item[field.FieldName] = ReadField(node, field.SemanticKeys);
			}

			output.Add(item);
		}

		return output;
	}

	private static JArray ExpandToNodes(string json)
	{
		var token = JToken.Parse(json);

		try
		{
			var options = new JsonLdOptions(string.Empty);
			var expanded = JsonLdProcessor.Expand(token, options);
			return JArray.FromObject(expanded);
		}
		catch
		{
			// Fallback when remote contexts cannot be resolved by the runtime.
			return token switch
			{
				JArray arr => arr,
				JObject obj => [obj],
				_ => []
			};
		}
	}

	private static List<string> ReadTypes(JObject node)
	{
		var types = new List<string>();

		if (node["@type"] is JArray expandedTypeArray)
		{
			foreach (var value in expandedTypeArray)
			{
				var typeValue = value.Value<string>();
				if (!string.IsNullOrWhiteSpace(typeValue))
				{
					types.Add(typeValue);
				}
			}

			return types;
		}

		if (node["@type"] is JValue compactedType)
		{
			var typeValue = compactedType.Value<string>();
			if (!string.IsNullOrWhiteSpace(typeValue))
			{
				types.Add(typeValue);
			}
		}

		return types;
	}

	private static object? ReadField(JObject node, IReadOnlyList<string> semanticKeys)
	{
		foreach (var key in semanticKeys)
		{
			foreach (var iri in ExpandSemanticKey(key))
			{
				if (!node.TryGetValue(iri, out var valueToken))
				{
					continue;
				}

				var value = ParseValueToken(valueToken);
				if (value is not null)
				{
					return value;
				}
			}

			// Fallback for compacted JSON-LD where fields are not expanded.
			var compactedProperty = key.Contains(':') ? key[(key.IndexOf(':') + 1)..] : key;
			if (node.TryGetValue(compactedProperty, out var compactedToken))
			{
				var compactedValue = ParseValueToken(compactedToken);
				if (compactedValue is not null)
				{
					return compactedValue;
				}
			}
		}

		return null;
	}

	private static IEnumerable<string> ExpandSemanticKey(string semanticKey)
	{
		if (PrefixMap.TryGetValue(semanticKey, out var iris))
		{
			return iris;
		}

		return [semanticKey];
	}

	private static object? ParseValueToken(JToken token)
	{
		if (token is JArray array && array.Count > 0)
		{
			return ParseValueToken(array[0]);
		}

		if (token is JObject obj)
		{
			if (obj.TryGetValue("@value", out var literalValue))
			{
				return ConvertJValue(literalValue);
			}

			if (obj.TryGetValue("@id", out var idValue))
			{
				return idValue.Value<string>();
			}
		}

		return ConvertJValue(token);
	}

	private static object? ConvertJValue(JToken token)
	{
		return token.Type switch
		{
			JTokenType.Integer => token.Value<long>(),
			JTokenType.Float => token.Value<double>(),
			JTokenType.Boolean => token.Value<bool>(),
			JTokenType.String => token.Value<string>(),
			_ => null
		};
	}
}