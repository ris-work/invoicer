using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MyAOTFriendlyExtensions
{
    /// <summary>
    /// A record struct representing field metadata for UI rendering.
    /// Description is a human‑friendly label.
    /// Value is the current property value.
    /// HandlerName is an optional string to drive UI input controls.
    /// </summary>
    public readonly record struct FieldData(string Description, object? Value, string? HandlerName);

    /// <summary>
    /// A simple registry that maps field names to UI handler strings.
    /// Call SetUIHandler to override default or later supply a custom handler.
    /// </summary>
    public static class UIHandlerRegistry
    {
        // Using a standard dictionary; in a multithreaded scenario consider thread‐safety.
        private static readonly Dictionary<string, string> _handlerOverrides = new();

        /// <summary>
        /// Sets the UI handler string for a given field name.
        /// </summary>
        /// <param name="fieldName">The field name as it appears in your JSON</param>
        /// <param name="handler">The UI handler identifier (for example, "DatePicker" or "TextInput")</param>
        public static void SetUIHandler(string fieldName, string handler)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name may not be null or whitespace.", nameof(fieldName));

            _handlerOverrides[fieldName] = handler;
        }

        /// <summary>
        /// Retrieves the UI handler for a given field, if one was registered.
        /// Returns null if none was registered.
        /// </summary>
        public static string? GetHandler(string fieldName)
        {
            return _handlerOverrides.TryGetValue(fieldName, out string handler) ? handler : null;
        }
    }

    /// <summary>
    /// Provides extension methods to convert objects to/from dictionaries without using reflection
    /// (other than the JSON source–generated conversion, which is AOT‑friendly).
    /// In addition, it offers an ApplyChangesFromFiltered method for fine‑grained updates.
    /// </summary>
    public static class JsonDictionaryExtensions
    {
        #region Basic Conversions Using JSON Round‑Trip (Assumes Source Generators)

        /// <summary>
        /// Converts an object into a flat dictionary based on its JSON representation.
        /// This yields a dictionary mapping property names to native simple values.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this object source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            // Serialize the object to JSON.
            // With source generators, this is AOT–friendly.
            string json = JsonSerializer.Serialize(source);
            // Convert JSON into a dictionary with JsonElement values.
            Dictionary<string, JsonElement>? temp =
                JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            var result = new Dictionary<string, object?>(temp?.Count ?? 0);
            if (temp is not null)
            {
                foreach (var kv in temp)
                {
                    result[kv.Key] = ConvertJsonElement(kv.Value);
                }
            }
            return result;
        }

        // Minimal helper that converts a JsonElement into a native .NET type.
        private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out int intVal) ? intVal :
                                     element.TryGetInt64(out long longVal) ? longVal :
                                     element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };

        /// <summary>
        /// Converts a flat dictionary (created via ToDictionary) back into an object of type T.
        /// This uses JSON as an intermediate and is AOT–friendly with source generators.
        /// </summary>
        public static T FromDictionary<T>(this Dictionary<string, object?> dictionary)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            string json = JsonSerializer.Serialize(dictionary);
            T? obj = JsonSerializer.Deserialize<T>(json);
            if (obj is null)
                throw new InvalidOperationException("Deserialization failed.");
            return obj;
        }

        #endregion

        #region Dictionary With UI Handler (Using the Registry)

        /// <summary>
        /// Converts an object into a dictionary of FieldData.
        /// Each entry includes:
        ///   • A human–friendly description (the property name by default)
        ///   • The property’s value (from JSON conversion)
        ///   • A UI handler string determined by the UIHandlerRegistry (or null if not set)
        /// </summary>
        public static Dictionary<string, FieldData> ToDictionaryWithHandler(this object source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            // Leverage the JSON round–trip to get property values.
            Dictionary<string, object?> baseDict = source.ToDictionary();
            var result = new Dictionary<string, FieldData>(baseDict.Count);
            foreach (var kv in baseDict)
            {
                // Instead of reflection-based heuristics, use our static registry override.
                string? uiHandler = UIHandlerRegistry.GetHandler(kv.Key);
                result.Add(kv.Key, new FieldData(kv.Key, kv.Value, uiHandler));
            }
            return result;
        }

        /// <summary>
        /// Reconstitutes an object of type T from a dictionary mapping property names to
        /// a tuple of (description, value). The description is ignored.
        /// This uses JSON–based deserialization, and thus does not rely on reflection.
        /// </summary>
        public static T FromDictionaryWithHandler<T>(this Dictionary<string, (string, object)> dict)
        {
            if (dict is null)
                throw new ArgumentNullException(nameof(dict));

            // Convert the input into a simpler dictionary mapping.
            var simpleDict = new Dictionary<string, object?>(dict.Count);
            foreach (var kv in dict)
            {
                simpleDict[kv.Key] = kv.Value.Item2;
            }
            return simpleDict.FromDictionary<T>();
        }

        #endregion

        #region Filtered Updates Using JSON (No Reflection)

        /// <summary>
        /// Creates an updated instance of T by copying changes from a JSON payload but only for
        /// keys specified in the filter array. Keys not in the filter are ignored.
        /// This is useful for fine–grained access control in CRUD operations.
        /// Since updating in–place without reflection is challenging in AOT,
        /// this method returns the new instance with applied changes.
        /// </summary>
        /// <typeparam name="T">Type to update (should be JSON–serializable via source generators)</typeparam>
        /// <param name="target">The original instance.</param>
        /// <param name="filter">The array of allowed property names.</param>
        /// <param name="json">The JSON payload containing property values (B).</param>
        /// <returns>A new instance of T with allowed changes applied.</returns>
        public static T ApplyChangesFromFiltered<T>(this T target, string[] filter, string json)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));
            if (string.IsNullOrWhiteSpace(json))
                return target;

            // Convert the target object to a dictionary.
            Dictionary<string, object?> targetDict = target.ToDictionary();

            // Deserialize the incoming JSON to a dictionary of JsonElement.
            Dictionary<string, JsonElement>? updateDict =
                JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (updateDict is null)
                return target;

            // For each allowed property (those in filter), override the value in the target dictionary.
            foreach (string key in filter)
            {
                if (updateDict.TryGetValue(key, out JsonElement elem))
                {
                    targetDict[key] = ConvertJsonElement(elem);
                }
            }
            // Produce a new instance from the merged dictionary.
            return targetDict.FromDictionary<T>();
        }

        #endregion
    }
}