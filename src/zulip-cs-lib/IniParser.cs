using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace zulip_cs_lib
{
    /// <summary>Utility wrapper to parse INI files, since that's what Zulip uses for bot info.</summary>
    public static class IniParser
    {
        /// <summary>The comment prefix characters.</summary>
        private static HashSet<char> _commentPrefixChars = new HashSet<char>() { '#', ';' };

        /// <summary>Attempts to get section data.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the requested file is not present.</exception>
        /// <param name="filename">   Filename of the file.</param>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="sectionData">[out] Information describing the section.</param>
        /// <returns>True if it succeeds, false if it fails.</returns>
        public static bool TryGetSectionDataFromFile(string filename, string sectionName, out Dictionary<string, string> sectionData)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Could not find specified INI file", filename);
            }

            if (string.IsNullOrEmpty(sectionName))
            {
                throw new ArgumentNullException(nameof(sectionName));
            }

            using (StreamReader reader = new StreamReader(filename))
            {
                sectionData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return ProcessStreamForSection(reader, sectionName, sectionData);
            }
        }

        /// <summary>Attempts to get section data.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="contents">   The contents.</param>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="sectionData">[out] Information describing the section.</param>
        /// <returns>True if it succeeds, false if it fails.</returns>
        public static bool TryGetSectionData(string contents, string sectionName, out Dictionary<string, string> sectionData)
        {
            if (string.IsNullOrEmpty(contents))
            {
                sectionData = null;
                return false;
            }

            if (string.IsNullOrEmpty(sectionName))
            {
                throw new ArgumentNullException(nameof(sectionName));
            }

            using (StringReader reader = new StringReader(contents))
            {
                sectionData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return ProcessStreamForSection(reader, sectionName, sectionData);
            }
        }

        /// <summary>Process the stream for section.</summary>
        /// <param name="reader">     The reader.</param>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="sectionData">[out] Information describing the section.</param>
        /// <returns>True if it succeeds, false if it fails.</returns>
        private static bool ProcessStreamForSection(TextReader reader, string sectionName, Dictionary<string, string> sectionData)
        {
            bool done = false;

            string formattedSectionName = $"[{sectionName}]";

            bool foundCorrectSection = false;

            while (!done)
            {
                string line = reader.ReadLine();

                if (line == null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!foundCorrectSection)
                {
                    if (line.Equals(formattedSectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundCorrectSection = true;
                    }

                    continue;
                }

                // check for next section marker
                if (line[0] == '[')
                {
                    return true;
                }

                // check for comments - ignore
                if (_commentPrefixChars.Contains(line[0]))
                {
                    continue;
                }

                // check for '=' as a separator
                if (line.Contains('=', StringComparison.Ordinal))
                {
                    string[] lineContents = line.Split('=');

                    // first part is our key, use the original string in case there were additional delimters in the value
                    sectionData.Add(lineContents[0].Trim(), line.Substring(lineContents[0].Length + 1).Trim());
                }
                else if (line.Contains(':', StringComparison.Ordinal))
                {
                    string[] lineContents = line.Split(':');

                    // first part is our key, use the original string in case there were additional delimters in the value
                    sectionData.Add(lineContents[0].Trim(), line.Substring(lineContents[0].Length + 1).Trim());
                }
            }

            return foundCorrectSection;
        }

    }
}
