using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RssFeeder.Models.Serialization;

public sealed class JObjectBsonSerializer : SerializerBase<JObject>
{
    public override void Serialize(
        BsonSerializationContext context,
        BsonSerializationArgs args,
        JObject value)
    {
        var document = BsonDocument.Parse(value.ToString(Formatting.None));
        BsonSerializer.Serialize(context.Writer, document);
    }

    public override JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        return JObject.Parse(document.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }));
    }
}
