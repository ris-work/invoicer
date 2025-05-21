using System;
using System.Collections.Generic;
using System.IO;
using MyAOTFriendlyExtensions;
using Tomlyn;
using Tomlyn.Model;

namespace CommonUi
{
    public static class TranslationHelper
    {
        public static string Lang = "en";

        // Holds the translations organized by language (e.g., "si", "ta", "hi", "fr")
        // and then by key (Item3 of your tuple)
        private static readonly Dictionary<string, Dictionary<string, string>> _translations =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads translations from the specified TOML file.
        /// </summary>
        /// <param name="filePath">The path to the translations TOML file.</param>
        public static void LoadTranslations(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Translation] File not found: {filePath}");
                return;
            }

            try
            {
                string tomlContent = File.ReadAllText(filePath);
                // Parse the TOML file using Tomlyn
                var parseResult = Tomlyn.Toml.Parse(tomlContent);

                // If there are parsing errors, log and skip loading translations.
                if (parseResult.HasErrors)
                {
                    Console.WriteLine("[Translation] Errors occurred while parsing the TOML file:");
                    foreach (var error in parseResult.Diagnostics)
                    {
                        Console.WriteLine(error.ToString());
                    }
                    return;
                }

                // Convert the parsed TOML model into a dictionary for easier lookup.
                var model = parseResult.ToModel();
                if (model != null)
                {
                    foreach (var langEntry in model)
                    {
                        // Each language section (like "si", "ta", etc.) should be a dictionary.
                        if (langEntry.Value is TomlTable langTranslations)
                        {
                            var keyValues = _translations.GetValueOrDefault(
                                langEntry.Key,
                                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            );
                            foreach (var kv in langTranslations)
                            {
                                keyValues[kv.Key] = (string)kv.Value ?? "";
                                System.Console.WriteLine($"Added: {kv.Key} => {kv.Value}");
                            }
                            System.Console.WriteLine($"Added: {langEntry.Key}");
                            _translations[langEntry.Key] = keyValues;
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Casting failed: {langEntry.Key}: {langEntry.Value}"
                            );
                        }
                    }
                    Console.WriteLine("[Translation] Translations loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Translation] Error while reading translations: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns the translated text for the specified key and language.
        /// If the language or the specific key is missing from the translations,
        /// the default text is returned.
        /// </summary>
        /// <param name="key">The key to look up (Item3 of the tuple).</param>
        /// <param name="defaultText">The default text (Item1 of the tuple) to use if no translation is found.</param>
        /// <param name="language">The language code (e.g., "si", "ta", "hi", "fr").</param>
        /// <returns>The translated string if found; otherwise, the default text.</returns>
        public static string Translate(string key, string defaultText, string language)
        {
            string result = defaultText;

            if (_translations.TryGetValue(language, out var langDict))
            {
                if (
                    langDict.TryGetValue(key, out var translatedValue)
                    && !string.IsNullOrWhiteSpace(translatedValue)
                )
                {
                    result = translatedValue;
                    Console.WriteLine(
                        $"[Translation] Lookup: key='{key}', language='{language}' → Found translation: '{result}'"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"[Translation] Lookup: key='{key}', language='{language}' → Entry not found. Using default: '{defaultText}'"
                    );
                }
            }
            else
            {
                Console.WriteLine(
                    $"[Translation] Lookup: key='{key}', language='{language}' → Language not loaded. Using default: '{defaultText}'"
                );
            }

            return result;
        }
    }
}
