using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Search.Infrastructure.Configuration;

public static class MongoSerializationConfiguration
{
    private static readonly object Sync = new();
    private static bool _configured;

    public static void Configure()
    {
        lock (Sync)
        {
            if (_configured)
            {
                return;
            }

            BsonSerializer.RegisterSerializer(
                new GuidSerializer(
                    GuidRepresentation.Standard));

            _configured = true;
        }
    }
}