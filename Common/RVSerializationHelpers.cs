using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MyAOTFriendlyExtensions
{
    // Define the Logger delegate.
    /// <summary>
    /// 
    /// Debug logging, set it to no-op in production
    /// 
    /// </summary>
    public delegate void Logger(string message);

    // A helper class to hold the default logger.
    public static class LogHelper
    {
        // The default logger writes messages to the console.
        public static Logger DefaultLogger = message => Console.WriteLine(message);
    }
    
    /// <summary>
    /// A record struct representing field metadata for UI rendering.
    /// Description is a friendly label, Value is the current property value,
    /// and HandlerName is an optional indicator of which UI widget to use.
    /// </summary>
    public readonly record struct FieldData(string Description, object? Value, string? HandlerName);

    /// <summary>
    /// A simple registry for UI handler overrides. Instead of using built‑in heuristics,
    /// you can set a custom handler for a given field.
    /// </summary>
    public static class UIHandlerRegistry
    {
        private static readonly Dictionary<string, string> _handlerOverrides = new();

        /// <summary>
        /// Manually specifies the UI handler for a given field.
        /// </summary>
        public static void SetUIHandler(string fieldName, string handler)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException(
                    "Field name may not be null or whitespace.",
                    nameof(fieldName)
                );
            _handlerOverrides[fieldName] = handler;
        }

        /// <summary>
        /// Retrieves the handler for the given field (if one was set); otherwise returns null.
        /// </summary>
        public static string? GetHandler(string fieldName) =>
            _handlerOverrides.TryGetValue(fieldName, out string handler) ? handler : null;
    }

    /// <summary>
    /// Provides extension methods that leverage JSON round‑trip conversions (assuming source generators)
    /// to work with EF Core entities in an AOT–friendly manner. This includes:
    /// • Converting objects to/from dictionaries,
    /// • Applying filtered updates,
    /// • Creating default JSON objects,
    /// • Performing single‑entry updates, and now
    /// • Removing fields from objects.
    /// </summary>
    public static class JsonDictionaryExtensions
    {
        #region Basic Conversions Using JSON Round‑Trip

        /// <summary>
        /// Converts an object into a flat dictionary by serializing it to JSON and then deserializing
        /// into a dictionary mapping property names to native values.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this object source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            string json = JsonSerializer.Serialize(source);
            Dictionary<string, JsonElement>? temp = JsonSerializer.Deserialize<
                Dictionary<string, JsonElement>
            >(json);
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

        // Helper to convert a JsonElement into a native .NET type (flat values only).
        private static object? ConvertJsonElement(JsonElement element) =>
            element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out int intVal) ? intVal
                : element.TryGetInt64(out long longVal) ? longVal
                : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString(),
            };

        /// <summary>
        /// Converts a flat dictionary (produced via ToDictionary) back into an object of type T.
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

        #region Dictionary With UI Handler

        /// <summary>
        /// Converts an object into a dictionary of FieldData.
        /// Each entry contains:
        ///   • A human‑friendly description (default: the property name),
        ///   • The property's value (from JSON conversion),
        ///   • A UI handler string (if one was set via UIHandlerRegistry).
        /// </summary>
        public static Dictionary<string, FieldData> ToDictionaryWithHandler(this object source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var baseDict = source.ToDictionary();
            var result = new Dictionary<string, FieldData>(baseDict.Count);
            foreach (var kv in baseDict)
            {
                string? uiHandler = UIHandlerRegistry.GetHandler(kv.Key);
                result.Add(kv.Key, new FieldData(kv.Key, kv.Value, uiHandler));
            }
            return result;
        }

        /// <summary>
        /// Reconstitutes an object of type T from a dictionary whose values are (description, value) tuples.
        /// The description is ignored.
        /// </summary>
        public static T FromDictionaryWithHandler<T>(this Dictionary<string, (string, object)> dict)
        {
            if (dict is null)
                throw new ArgumentNullException(nameof(dict));

            var simpleDict = new Dictionary<string, object?>(dict.Count);
            foreach (var kv in dict)
            {
                simpleDict[kv.Key] = kv.Value.Item2;
            }
            return simpleDict.FromDictionary<T>();
        }

        #endregion

        #region Filtered Updates for Fine‑Grained ACL Control

        /// <summary>
        /// Creates a new instance of T by copying only the allowed keys (provided in filter)
        /// from a JSON payload. Keys not in filter are ignored.
        /// This is useful for ACL–based updates.
        /// </summary>
        public static T ApplyChangesFromFiltered<T>(this T target, string[] filter, string json)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));
            if (string.IsNullOrWhiteSpace(json))
                return target;

            Dictionary<string, JsonElement>? updateDict = JsonSerializer.Deserialize<
                Dictionary<string, JsonElement>
            >(json);
            if (updateDict is null)
                return target;

            Dictionary<string, object?> targetDict = target.ToDictionary();
            foreach (string key in filter)
            {
                if (updateDict.TryGetValue(key, out JsonElement elem))
                {
                    targetDict[key] = ConvertJsonElement(elem);
                }
            }
            return targetDict.FromDictionary<T>();
        }

        public static T ApplyChangesFromFiltered<T>(this T target, string[] filter, T jsonSerializableObject)
        {
            return target.ApplyChangesFromFiltered(filter, JsonSerializer.Serialize(jsonSerializableObject));
            
        }

        #endregion

        #region Create Default Object JSON

        /// <summary>
        /// Creates a JSON string for a default instance of type T using default field values.
        /// Optionally, you can specify an ID field to override its default value.
        /// </summary>
        /// <typeparam name="T">The EF Core entity type to default (must have a parameterless constructor).</typeparam>
        /// <param name="idField">The name of the ID field to override (default is "id").</param>
        /// <param name="idDefault">The value to assign to the ID field. If not provided, null is used.</param>
        /// <returns>A JSON string representing the default object.</returns>
        public static string CreateDefaultObject<T>(string idField = "id", object? idDefault = null)
            where T : new()
        {
            T instance = new T();
            var dict = instance.ToDictionary();
            if (dict.ContainsKey(idField))
            {
                dict[idField] = idDefault;
            }
            return JsonSerializer.Serialize(dict);
        }

        #endregion

        #region IEnumerable Extension for Single‑Entry Updates

        /// <summary>
        /// Updates a collection of EF Core tracked entities using an update object.
        /// This method works on an IEnumerable&lt;T&gt; and only runs the update
        /// if exactly one entity is present. It merges properties from the update object into
        /// the original (ignoring the ID field).
        /// </summary>
        /// <typeparam name="T">The entity type (must have a parameterless constructor).</typeparam>
        /// <param name="source">A filtered sequence expected to contain exactly one element.</param>
        /// <param name="update">The update object containing new values (ID field is ignored).</param>
        /// <param name="idField">The name of the ID field to ignore (default is "id").</param>
        /// <returns>The updated entity (a new instance with merged values).</returns>
        public static T UpdateIfOne<T>(this IEnumerable<T> source, T update, string idField = "id")
            where T : class, new()
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (update is null)
                throw new ArgumentNullException(nameof(update));

            var list = source.ToList();
            if (list.Count != 1)
                throw new InvalidOperationException(
                    "UpdateIfOne requires exactly one element in the sequence."
                );

            T original = list[0];
            var origDict = original.ToDictionary();
            var updateDict = update.ToDictionary();
            updateDict.Remove(idField); // Ensure the ID field is not copied over.
            foreach (var kv in updateDict)
            {
                origDict[kv.Key] = kv.Value;
            }
            return origDict.FromDictionary<T>();
        }

        #endregion

        #region Additional Extension: RemoveField for Any JSON Serializable Object

        /// <summary>
        /// Removes the specified field from the object by converting it to a dictionary,
        /// deleting the provided key, and rehydrating the object as a new instance.
        /// This enables using, for example:
        ///    Product newProduct = product.RemoveField("id");
        /// which returns a new instance lacking that field.
        /// </summary>
        /// <typeparam name="T">The type of the JSON serializable object.</typeparam>
        /// <param name="target">The object from which to remove the field.</param>
        /// <param name="fieldName">The name of the field to remove.</param>
        /// <returns>A new instance of T without the specified field.</returns>
        public static T RemoveField<T>(this T target, string fieldName)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            var dict = target.ToDictionary();
            dict.RemoveField(fieldName); // Uses the dictionary extension below.
            return dict.FromDictionary<T>();
        }

        /// <summary>
        /// Removes the specified field from the dictionary (in place) and returns the dictionary.
        /// Useful for unconditional filtering (e.g. stripping out an "id" field).
        /// </summary>
        /// <typeparam name="T">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary from which to remove the field.</param>
        /// <param name="fieldName">The field name to remove.</param>
        /// <returns>The modified dictionary.</returns>
        public static Dictionary<string, T> RemoveField<T>(
            this Dictionary<string, T> dict,
            string fieldName
        )
        {
            if (dict is null)
                throw new ArgumentNullException(nameof(dict));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException(
                    "Field name cannot be null or whitespace.",
                    nameof(fieldName)
                );
            dict.Remove(fieldName);
            return dict;
        }

        #endregion
        #region AOT-Friendly Conversion Methods

        /// <summary>
        /// Converts an object to a Dictionary&lt;string, JsonElement&gt; by serializing it to JSON 
        /// and then deserializing that JSON. This avoids reflection and is AOT friendly.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <returns>A dictionary representing the JSON properties of the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        public static Dictionary<string, JsonElement> ToJsonDictionary<T>(this T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            string json = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, JsonElement&gt; back into an object of type T by serializing
        /// the dictionary to JSON and deserializing the JSON.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="dict">The dictionary representing the object.</param>
        /// <returns>An object of type T.</returns>
        /// <exception cref="ArgumentNullException">Thrown when dict is null.</exception>
        public static T FromJsonDictionary<T>(this Dictionary<string, JsonElement> dict)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            string json = JsonSerializer.Serialize(dict);
            return JsonSerializer.Deserialize<T>(json);
        }

        #endregion

        #region Dictionary Removal Helper

        /// <summary>
        /// Removes the specified field from the dictionary if it exists, using a case‑insensitive comparison.
        /// Logs the removal using the provided Logger.
        /// </summary>
        /// <param name="dict">The dictionary from which to remove the field.</param>
        /// <param name="fieldName">The field name to remove.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <returns>The updated dictionary.</returns>
        /// <exception cref="ArgumentNullException">Thrown when dict or logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown when fieldName is null or whitespace.</exception>
        public static Dictionary<string, JsonElement> RemoveFieldFromDictionaryIfPresent(
            this Dictionary<string, JsonElement> dict,
            string fieldName,
            Logger? logger = null)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
            if (logger == null)
                logger = LogHelper.DefaultLogger;



            string keyToRemove = null;
            // Use an explicit loop to ensure AOT compatibility (avoiding LINQ)
            foreach (var key in dict.Keys)
            {
                if (string.Equals(key, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    keyToRemove = key;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(keyToRemove))
            {
                dict.Remove(keyToRemove);
                logger($"Removed field: '{keyToRemove}'");
            }

            return dict;
        }

        #endregion

        #region Object Extension: RemoveFieldIfPresent

        /// <summary>
        /// Removes the specified field from the target object by serializing it to JSON, 
        /// converting to a dictionary, removing the field (case‑insensitively),
        /// and then deserializing the modified dictionary back into the target type.
        /// This method is AOT friendly assuming that serializer code generation is present.
        /// </summary>
        /// <typeparam name="T">The type of the target object.</typeparam>
        /// <param name="target">The target object.</param>
        /// <param name="fieldName">The field to remove.</param>
        /// <param name="logger">The logger to use for logging events.</param>
        /// <returns>The updated object of type T.</returns>
        /// <exception cref="ArgumentNullException">Thrown when target or logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown when fieldName is null or whitespace.</exception>
        public static T RemoveFieldIfPresent<T>(this T target, string fieldName, Logger? logger = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
            if (logger == null)
                logger = LogHelper.DefaultLogger;

            // Convert the target object to a dictionary.
            var dict = target.ToJsonDictionary();
            dict.RemoveFieldFromDictionaryIfPresent(fieldName, logger);

            // Convert the modified dictionary back into an object of type T.
            return dict.FromJsonDictionary<T>();
        }

        #endregion

        #region JSON Update Method

        /// <summary>
        /// Updates the target object by removing fields specified in the removalFilter.
        /// For each key in the removalFilter that exists in both the JSON update dictionary
        /// and the target object, the field is removed. Each removal event is logged using the provided Logger.
        /// </summary>
        /// <typeparam name="T">The type of the target object.</typeparam>
        /// <param name="target">The object to update.</param>
        /// <param name="removalFilter">An array of field names to remove.</param>
        /// <param name="json">A JSON string representing the update dictionary.</param>
        /// <param name="logger">The logger to use for logging events.</param>
        /// <returns>The updated object of type T.</returns>
        public static T ApplyChangesExceptFilteredFromJson<T>(
    this T target,
    string[] removalFilter,
    string patchJson,
    Logger? logger = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (removalFilter == null)
                throw new ArgumentNullException(nameof(removalFilter));
            if (logger == null)
                logger = LogHelper.DefaultLogger; // Assumes DefaultLogger is defined elsewhere
            if (string.IsNullOrWhiteSpace(patchJson))
                return target;

            // Deserialize the patch JSON into a dictionary.
            Dictionary<string, JsonElement>? patchDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(patchJson);
            if (patchDict == null)
                throw new InvalidOperationException("Failed to deserialize update patch JSON.");

            // Remove any disallowed fields from the patch (case‑insensitively).
            foreach (string field in removalFilter)
            {
                if (string.IsNullOrWhiteSpace(field))
                    continue;

                string? keyToRemove = null;
                foreach (string key in patchDict.Keys)
                {
                    if (string.Equals(key, field, StringComparison.OrdinalIgnoreCase))
                    {
                        keyToRemove = key;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(keyToRemove))
                {
                    logger($"Filtering out field '{keyToRemove}' from update patch due to ACL restrictions.");
                    patchDict.Remove(keyToRemove);
                }
            }

            // Convert the target object to a dictionary.
            string targetJson = JsonSerializer.Serialize(target);
            Dictionary<string, JsonElement>? targetDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(targetJson);
            if (targetDict == null)
                throw new InvalidOperationException("Failed to deserialize target object.");

            // Merge the filtered patch into the target dictionary.
            foreach (var kvp in patchDict)
                targetDict[kvp.Key] = kvp.Value;

            // Convert the merged dictionary back to an object of type T.
            string mergedJson = JsonSerializer.Serialize(targetDict);
            return JsonSerializer.Deserialize<T>(mergedJson);
        }

        public static T ApplyChangesExceptFiltered<T>(
            this T target,
            string[] removalFilter,
            T updatePatch,
            Logger? logger = null)
        {
            // Serialize the update patch to JSON and delegate to the JSON-based version.
            string patchJson = JsonSerializer.Serialize(updatePatch);
            return target.ApplyChangesExceptFilteredFromJson(removalFilter, patchJson, logger);
        }


        public static T ApplyFilteredUpdate<T>(
            this T target,
            T updateObject,
            string[] removalFilter,
            Logger? logger = null)
        {
            // This helper simply calls the above method.
            return target.ApplyChangesExceptFiltered(removalFilter, updateObject, logger);
        }


        #endregion
    }
}
