using System.Net;
using Microsoft.Azure.Cosmos;
using PracticeAgent.Services.Interfaces;
using Newtonsoft.Json;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Collections.Generic;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using Newtonsoft.Json.Linq;

namespace PracticeAgent.Services
{
    public class CosmosService: ICosmosService
    {        
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseId = String.Empty;

        public CosmosService(IConfiguration configuration)
        {
            // Read Cosmos DB configuration from appsettings.json
            var cosmosEndpoint = configuration["CosmosDb:Endpoint"];
            var cosmosKey = configuration["CosmosDb:Key"];
            _databaseId = configuration["CosmosDb:DatabaseId"] ?? string.Empty;

            // Configure custom serialization options
            var cosmosClientOptions = new CosmosClientOptions
            {
                Serializer = new CustomJsonCosmosSerializer()
            };

            _cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey, cosmosClientOptions);
        }

        public async Task<List<T>> ReadItemAsync<T>(string containerId, string id)
        {
            try
            {
                Container _container = _cosmosClient.GetDatabase(_databaseId).GetContainer(containerId);
                var response = await _container.ReadItemAsync<T>(id, new PartitionKey(id));
                return new List<T> { response.Resource };
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Handle not found case
                return null;
            }
            catch (JsonSerializationException ex)
            {
                // Handle serialization issues
                Console.WriteLine($"Serialization error: {ex.Message}");
                throw new ApplicationException("Failed to deserialize data from Cosmos DB. The data format may have changed or contains abstract types.", ex);
            }
        }

        public async Task<bool> UpsertItemAsync<T>(string containerId, T item, string id)
        {
            // Ensure the item has an ID
            Container _container = _cosmosClient.GetDatabase(_databaseId).GetContainer(containerId);
            await _container.UpsertItemAsync(item, new PartitionKey(id));
            return true;
        }
    }

    // Custom serializer that handles Semantic Kernel types correctly
    public class CustomJsonCosmosSerializer : CosmosSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public CustomJsonCosmosSerializer()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                // Add converters for specific types if needed
                Converters = new List<JsonConverter>
                {
                    new KernelContentConverter()
                }
            };
        }

        public override T FromStream<T>(Stream stream)
        {
            if (stream == null || stream.Length == 0)
            {
                return default;
            }

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                try
                {
                    var result = JsonSerializer.Create(_settings).Deserialize<T>(jtr);
                    if (result == null)
                    {
                        throw new JsonSerializationException("Deserialization returned null.");
                    }
                    return result;
                }
                catch (JsonSerializationException ex)
                {
                    Console.WriteLine($"Error deserializing Cosmos DB response: {ex.Message}");
                    throw;
                }
            }
        }

        public override Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            using (var sw = new StreamWriter(stream, leaveOpen: true))
            using (var jtw = new JsonTextWriter(sw))
            {
                JsonSerializer.Create(_settings).Serialize(jtw, input);
                jtw.Flush();
                sw.Flush();
            }
            
            stream.Position = 0;
            return stream;
        }
    }

    // Custom converter for KernelContent interface
    public class KernelContentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(KernelContent) || objectType.IsAssignableFrom(typeof(KernelContent));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Implement concrete type deserialization based on your actual data model
            // For example, if you're using a specific implementation:
            if (reader.TokenType == JsonToken.Null)
                return null;

            // Replace with your actual concrete implementation of KernelContent
            // This is a placeholder - you need to use your actual concrete class
            return new TextContent(ReadStringContent(reader));
        }

        private string ReadStringContent(JsonReader reader)
        {
            // Basic string extraction from the JSON reader
            if (reader.TokenType == JsonToken.String)
                return reader.Value?.ToString() ?? string.Empty;
                
            var obj = JObject.Load(reader);
            return obj["Text"]?.ToString() ?? string.Empty;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // Just serialize the object as normal
            serializer.Serialize(writer, value);
        }
    }
}