using System;
using System.IO;
using System.Reflection;
using zulip_cs_lib;

namespace zulip_cs_cli
{
    /// <summary>A utility CLI for zulip-cs-lib.</summary>
    public static class Program
    {
        /// <summary>Main entry-point for this application.</summary>
        /// <param name="zuliprcFilename">An array of command-line argument strings.</param>
        static int Main(
            string zuliprcFilename = "")
        {
            if (string.IsNullOrEmpty(zuliprcFilename))
            {
                string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                zuliprcFilename = FindZulipRC(basePath);
            }

            if (!File.Exists(zuliprcFilename))
            {
                Console.WriteLine($"Could not find file: {zuliprcFilename}");
                return 1;
            }

            ZulipClient client = new ZulipClient(zuliprcFilename);

            ZulipResponse response = client.SendPrivateMessage(
                $"C# test - {DateTime.Now}", 
                "gino.canessa@microsoft.com").Result;

            Console.WriteLine(
                $"Post message attempt:\n" +
                $" HTTP Response: {response.HttpResponseCode}\n" +
                $"     HTTP Body: {response.HttpResponseBody}\n" +
                $"    Message ID: {response.Id}\n" +
                $"       Message: {response.Message}");

            return 0;
        }

        /// <summary>Searches for the first zulip RC file.</summary>
        /// <exception cref="DirectoryNotFoundException">Thrown when the requested directory is not
        ///  present.</exception>
        /// <param name="startingDir">The starting dir.</param>
        /// <returns>The found zulip rectangle.</returns>
        public static string FindZulipRC(string startingDir)
        {
            string currentDir = startingDir;
            string filePath = Path.Combine(currentDir, "zuliprc");

            while (!File.Exists(filePath))
            {
                // check for /temp/.zuliprc
                string pathInSubdir = Path.Combine(currentDir, "secrets", "zuliprc");

                if (File.Exists(pathInSubdir))
                {
                    return pathInSubdir;
                }

                currentDir = Path.GetFullPath(Path.Combine(currentDir, ".."));

                if (currentDir == Path.GetPathRoot(currentDir))
                {
                    throw new DirectoryNotFoundException("Could not find spec directory in path!");
                }

                filePath = Path.Combine(currentDir, "zuliprc");
            }

            return filePath;
        }

    }
}
