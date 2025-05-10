using System;
using System.IO;
using System.Reflection;

namespace CommonUi
{
    public static class ResourceExtractor
    {
        /// <summary>
        /// Ensures that the translations.toml file exists in the specified output path.
        /// If not, it extracts the file from the embedded resources.
        /// </summary>
        /// <param name="outputPath">The full file path where translations.toml should reside.</param>
        public static void EnsureTranslationsFile(string outputPath = "translations.toml")
        {
            if (File.Exists(outputPath))
            {
                Console.WriteLine($"[ResourceExtractor] {outputPath} already exists.");
                return;
            }

            // Get the current assembly.
            var assembly = Assembly.GetExecutingAssembly();

            // The resource name is usually the default namespace plus the file name.
            // For example, if your default namespace is "MyApp", the resource name is "MyApp.translations.toml".
            string resourceName = $"{assembly.GetName().Name}.translations.toml";
            Console.WriteLine($"[ResourceExtractor] Looking for embedded resource: {resourceName}");

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    Console.WriteLine($"[ResourceExtractor] ERROR: Embedded resource not found: {resourceName}");
                    return;
                }

                // Create the output file and copy the contents.
                using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            Console.WriteLine($"[ResourceExtractor] {outputPath} extracted successfully.");
        }
    }
}