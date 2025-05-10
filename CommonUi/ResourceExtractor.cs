using System;
using System.IO;
using System.Reflection;

namespace CommonUi
{
    public static class ResourceExtractor
    {
        /// <summary>
        /// Extracts the specified embedded resource if the target file does not exist.
        /// </summary>
        /// <param name="resourceFilename">The name of the resource file (e.g., "translations.toml", "Gourier.ttf").</param>
        public static void EnsureResource(string resourceFilename)
        {
            if (File.Exists(resourceFilename))
            {
                Console.WriteLine($"[ResourceExtractor] {resourceFilename} already exists.");
                return;
            }

            // Get the current assembly.
            var assembly = Assembly.GetExecutingAssembly();

            // Construct the resource name based on the assembly's default namespace.
            // If your embedded resources are organized in subfolders, adjust this to include the folder path.
            string resourceName = $"{assembly.GetName().Name}.{resourceFilename}";
            Console.WriteLine($"[ResourceExtractor] Looking for embedded resource: {resourceName}");

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    Console.WriteLine(
                        $"[ResourceExtractor] ERROR: Embedded resource not found: {resourceName}"
                    );
                    return;
                }

                // Create and write the output file.
                using (
                    var fileStream = new FileStream(
                        resourceFilename,
                        FileMode.Create,
                        FileAccess.Write
                    )
                )
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            Console.WriteLine($"[ResourceExtractor] {resourceFilename} extracted successfully.");
        }

        /// <summary>
        /// Backwards compatibility polyfill for older code that directly calls EnsureTranslationsFile.
        /// This method redirects to the generic EnsureResource method.
        /// </summary>
        /// <param name="outputPath">The full file path where translations.toml should reside.</param>
        [Obsolete(
            "Use EnsureResource instead. This method is maintained for backwards compatibility."
        )]
        public static void EnsureTranslationsFile(string outputPath = "translations.toml")
        {
            EnsureResource(outputPath);
        }

        /// <summary>
        /// Ensures that all required embedded resources are extracted.
        /// </summary>
        public static void EnsureAllResources()
        {
            // Using the polyfilled method for translations.toml.
            EnsureTranslationsFile();
            // Extract additional resources.
            EnsureResource("Gourier.ttf");
            EnsureResource("FluentEmoji.ttf");
        }
    }
}
