using System;
using System.Text.RegularExpressions;

namespace EmojiRemover
{
    public class SMPRemover
    {
        /// <summary>
        /// Removes characters that are encoded as surrogate pairs.
        /// This captures most emoji, which are typically outside the BMP.
        /// </summary>
        /// <param name="input">Input string that may contain emojis.</param>
        /// <returns>The string with surrogate pair characters removed.</returns>
        public static string RemoveEmojis(string input)
        {
            // The regex pattern matches a high surrogate followed by a low surrogate.
            return Regex.Replace(input, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", "");
        }

        /// <summary>
        /// Removes any character in the Supplementary Multilingual Plane (SMP).
        /// This explicitly targets Unicode code points from U+10000 to U+10FFFF.
        /// </summary>
        /// <param name="input">Input string that may contain SMP characters.</param>
        /// <returns>The string with SMP characters removed.</returns>
        public static string RemoveSMPCharacters(string input)
        {
            // This pattern matches any code point from U+10000 to U+10FFFF.
            // Note: Some environments may require RegexOptions or proper handling for inline Unicode escapes.
            return Regex.Replace(input, @"\p{Cs}", "");
        }
    }
}
