using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS

namespace EtoFE
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Resources;
    using System.Windows;

    public static class ResourceHelper
    {
        /// <summary>
        /// Recursively prints the contents of a ResourceDictionary, including its Source URI and merged dictionaries.
        /// </summary>
        /// <param name="dict">The ResourceDictionary to inspect.</param>
        /// <param name="indent">String used for indenting nested entries.</param>
        public static void PrintResourceDictionary(ResourceDictionary dict, string indent = "")
        {
            // Print the Source URI if one is defined.
            if (dict.Source != null)
            {
                Console.WriteLine($"{indent}Source: {dict.Source}");
            }
            else
            {
                Console.WriteLine($"{indent}[No Source]");
            }

            // Optionally, print out the keys contained directly in this dictionary.
            foreach (var key in dict.Keys)
            {
                // For brevity, we're printing just the keys.
                Console.WriteLine($"{indent}Key: {key}");
            }

            // Now look at any merged dictionaries.
            foreach (var merged in dict.MergedDictionaries)
            {
                if (merged is ResourceDictionary rd)
                {
                    Console.WriteLine($"{indent}Merged Dictionary:");
                    // Recursively print the merged dictionary with increased indent.
                    PrintResourceDictionary(rd, indent + "  ");
                }
            }
        }

        /// <summary>
        /// Prints all resource dictionaries currently merged into the Application's resources.
        /// </summary>
        public static void PrintAllApplicationResourceDictionaries()
        {
            if (Application.Current == null)
            {
                Console.WriteLine("Application.Current is null!");
                return;
            }

            Console.WriteLine("Application Resource Dictionaries:");
            PrintResourceDictionary(Application.Current.Resources);
        }
    }

    public static class ResourceDebugger
    {
        /// <summary>
        /// Lists the manifest resource names in the executing assembly.
        /// For XAML files set as Page, the build system generates a .g.resources file which holds the compiled BAML.
        /// </summary>
        public static void ListManifestResourceNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine("Assembly manifest resource names:");
            string[] names = assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                Console.WriteLine("  " + name);
            }
        }

        /// <summary>
        /// If a .g.resources file exists, enumerate its keys.
        /// This typically contains entries corresponding to XAML files compiled as Page.
        /// </summary>
        public static void ListGResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] names = assembly.GetManifestResourceNames();
            foreach (var resourceName in names)
            {
                if (resourceName.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Found g.resources: " + resourceName);
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (ResourceReader reader = new ResourceReader(stream))
                            {
                                IDictionaryEnumerator enumerator = reader.GetEnumerator();
                                while (enumerator.MoveNext())
                                {
                                    Console.WriteLine("  Resource key: " + enumerator.Key);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to get a resource stream for the given pack URI.
        /// Logs whether the stream was found or not.
        /// </summary>
        public static void TestResourceStream(Uri uri)
        {
            try
            {
                var resourceInfo = Application.GetResourceStream(uri);
                if (resourceInfo != null)
                {
                    Console.WriteLine("Resource found at: " + uri);
                }
                else
                {
                    Console.WriteLine("Resource NOT found at: " + uri);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting resource stream for " + uri);
                Console.WriteLine(ex);
            }
        }
    }
}
#endif
