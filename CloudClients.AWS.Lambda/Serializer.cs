using System.Text.Json;

namespace CloudClients.AWS.Lambda
{
    public static class Serializer
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions {  WriteIndented = true };

        public static string Serialize<TValue>(TValue value) => JsonSerializer.Serialize(value, _serializerOptions);

        public static TValue Deserialize<TValue>(string json) => string.IsNullOrEmpty(json) ? 
            default : 
            JsonSerializer.Deserialize<TValue>(json, _serializerOptions);
    }

}
