using Microsoft.Xna.Framework.Content;
using System.Text.Json;

namespace GDEngine.Core.Serialization
{
    public static class JSONSerializationUtility
    {
        /// <summary>
        /// Loads JSON as a list of <typeparamref name="T"/>. Supports:
        /// 1) Top-level array: [ {...}, {...} ]
        /// 2) Wrapped object with an inner array: { "version": 1, "meta": {...}, "<arrayPropertyName>": [ {...} ] }
        /// </summary>
        /// <see cref="Vector3JsonConverter"/>
        public static List<T> LoadData<T>(ContentManager content, string relativePath, string arrayPropertyName = "spawns")
        {
            string path = Path.Combine(content.RootDirectory, relativePath);
            string json = File.ReadAllText(path);

            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            opts.Converters.Add(new Vector3JsonConverter());

            int i = 0;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            bool isArray = i < json.Length && json[i] == '[';
            bool isObject = i < json.Length && json[i] == '{';

            if (isArray)
            {
                var many = JsonSerializer.Deserialize<List<T>>(json, opts);
                if (many != null)
                    return many;

                return new List<T>();
            }

            if (isObject)
            {
                using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                var root = doc.RootElement;

                // Try to find the inner array (e.g., "spawns") in a case-insensitive way.
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, arrayPropertyName, StringComparison.OrdinalIgnoreCase)
                            && prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            // Deserialize the inner array to List<T>
                            var innerArrayJson = prop.Value.GetRawText();
                            var many = JsonSerializer.Deserialize<List<T>>(innerArrayJson, opts);
                            if (many != null)
                                return many;

                            return new List<T>();
                        }
                    }
                }

                // Fallback: attempt to treat the object as a single T and wrap into a list (old behavior).
                var one = JsonSerializer.Deserialize<T>(json, opts);
                var list = new List<T>();
                if (one != null)
                    list.Add(one);

                return list;
            }

            // Fallback for empty/invalid content.
            return new List<T>();
        }
    }
}
