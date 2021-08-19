using System;
using System.IO;
using System.Text;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Static class that adds extension methods for the StringBuilder, used mainly in the shader generator for writing down the shader file
    /// </summary>
    public static class ShaderStringBuilderExtensions
    {
        /// <summary>
        /// Prepends a string to the StringBuilder.
        /// </summary>
        /// <param name="builder">Builder to use</param>
        /// <param name="value">string to prepend</param>
        /// <returns>The StringBuilder used</returns>
        public static StringBuilder Prepend(this StringBuilder builder, string value) => builder.Insert(0, value);

        /// <summary>
        /// Prepends a line to the StringBuilder.
        /// </summary>
        /// <param name="builder">Builder to use</param>
        /// <param name="value">string to prepend</param>
        /// <returns>The StringBuilder used</returns>
        public static StringBuilder PrependLine(this StringBuilder builder, string value) => builder.Prepend(Environment.NewLine).Prepend(value);
        
        /// <summary>
        /// Appends a line to the StringBuilder with a specific tab level attached.
        /// </summary>
        /// <param name="builder">Builder to use</param>
        /// <param name="tabLevel">number of tabs</param>
        /// <param name="value">string to append</param>
        /// <returns>The StringBuilder used</returns>
        public static StringBuilder AppendLineTabbed(this StringBuilder builder, int tabLevel, string value)
        {
            return builder.Append(Tabs(tabLevel)).AppendLine(value);
        }
        
        /// <summary>
        /// Prepends a line to the StringBuilder with a specific tab level attached.
        /// </summary>
        /// <param name="builder">Builder to use</param>
        /// <param name="tabLevel">number of tabs</param>
        /// <param name="value">string to prepend</param>
        /// <returns>The StringBuilder used</returns>
        public static StringBuilder PrependLineTabbed(this StringBuilder builder, int tabLevel, string value)
        {
            return builder.PrependLine(value).Prepend(Tabs(tabLevel));
        }
        
        /// <summary>
        /// Appends a string to the StringBuilder with a specific tab level attached.
        /// </summary>
        /// <param name="builder">Builder to use</param>
        /// <param name="tabLevel">number of tabs</param>
        /// <param name="value">string to append</param>
        /// <returns>The StringBuilder used</returns>
        public static StringBuilder AppendTabbed(this StringBuilder builder, int tabLevel, string value)
        {
            return builder.Append(Tabs(tabLevel)).Append(value);
        }
        
        /// <summary>
        /// Prepends a string to the StringBuilder with a specific tab level attached.
        /// </summary>
        /// <param name="builder">Builder to use</param>
        /// <param name="tabLevel">number of tabs</param>
        /// <param name="value">string to prepend</param>
        /// <returns>The StringBuilder used</returns>
        public static StringBuilder PrependTabbed(this StringBuilder builder, int tabLevel, string value)
        {
            return builder.Prepend(value).Prepend(Tabs(tabLevel));
        }

        /// <summary>
        /// Appends mulltiple lines to the StringBuilder with a specific tab level attached.
        /// </summary>
        /// <param name="builder">Builder to use</param>
        /// <param name="tabLevel">number of tabs</param>
        /// <param name="value">multiline string to append</param>
        /// <returns>The StringBuilder used</returns>
        public static StringBuilder AppendMultilineTabbed(this StringBuilder builder, int tabLevel, string value)
        {
            var sr = new StringReader(value);
            string line;
            while ((line = sr.ReadLine()) != null)
                builder.AppendLineTabbed(tabLevel, line);
            return builder;
        }

        /// <summary>
        /// Generate a string with the wanted amount of tabs.
        /// </summary>
        /// <param name="n">Number of tabs</param>
        /// <returns>string with the selected amount of tabs</returns>
        static string Tabs(int n)
        {
            if (n < 0) n = 0;
            return new string('\t', n);
        }

        /// <summary>
        /// Checks if a StringBuilder contains a specific string
        /// </summary>
        /// <remarks>
        /// This has been shamelessly copy pasted from here: https://stackoverflow.com/questions/12261344/fastest-search-method-in-stringbuilder
        /// </remarks>
        /// <param name="haystack">The StringBuilder to use</param>
        /// <param name="needle">The string to search</param>
        /// <returns>True it The StringBuilder contains the string, false otherwise</returns>
        public static bool Contains(this StringBuilder haystack, string needle)
        {
            return haystack.IndexOf(needle) != -1;
        }
        
        /// <summary>
        /// Get the index of the first match of a string
        /// </summary>
        /// <remarks>
        /// This has been shamelessly copy pasted from here: https://stackoverflow.com/questions/12261344/fastest-search-method-in-stringbuilder
        /// </remarks>
        /// <param name="haystack">The StringBuilder to use</param>
        /// <param name="needle">The string to search</param>
        /// <returns>The index of the first match</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static int IndexOf(this StringBuilder haystack, string needle)
        {
            if (haystack == null || needle == null)
                throw new ArgumentNullException();
            if (needle.Length == 0)
                return 0;//empty strings are everywhere!
            if (needle.Length == 1)//can't beat just spinning through for it
            {
                char c = needle[0];
                for (int idx = 0; idx != haystack.Length; ++idx)
                    if (haystack[idx] == c)
                        return idx;
                return -1;
            }
            int m = 0;
            int i = 0;
            int[] T = KmpTable(needle);
            while (m + i < haystack.Length)
            {
                if (needle[i] == haystack[m + i])
                {
                    if (i == needle.Length - 1)
                        return m == needle.Length ? -1 : m;//match -1 = failure to find conventional in .NET
                    ++i;
                }
                else
                {
                    m = m + i - T[i];
                    i = T[i] > -1 ? T[i] : 0;
                }
            }
            return -1;
        }
        private static int[] KmpTable(string sought)
        {
            int[] table = new int[sought.Length];
            int pos = 2;
            int cnd = 0;
            table[0] = -1;
            table[1] = 0;
            while (pos < table.Length)
                if (sought[pos - 1] == sought[cnd])
                    table[pos++] = ++cnd;
                else if (cnd > 0)
                    cnd = table[cnd];
                else
                    table[pos++] = 0;
            return table;
        }
    }
}