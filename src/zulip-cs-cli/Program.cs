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
                string basePath = AppContext.BaseDirectory;

                zuliprcFilename = FindZulipRC(basePath);
            }

            if (!File.Exists(zuliprcFilename))
            {
                Console.WriteLine($"Could not find file: {zuliprcFilename}");
                return 1;
            }

            ZulipClient client = new ZulipClient(zuliprcFilename, @"C:\Windows\system32\curl.exe");

            //ulong editMessageId = 244487707;

            //(bool success, string details) editResponse = client.Messages.TryEdit(
            //    editMessageId,
            //    $"Edited at: {DateTime.Now}").Result;

            //Console.WriteLine(
            //    $"Edit message attempt:\n" +
            //    $" success: {editResponse.success}\n" +
            //    $" details: {editResponse.details}");

            (bool success, string details, ulong messageId) sendResponse = client.Messages.TrySendPrivate(
                $"C# test - {DateTime.Now}",
                "gino.canessa@microsoft.com").Result;

            Console.WriteLine(
                $"Send private message attempt:\n" +
                $" success: {sendResponse.success}\n" +
                $" details: {sendResponse.details}\n" +
                $"      id: {sendResponse.messageId}");

            //(bool success, string details, ulong messageId) sendResponse = client.Messages.TrySendStream(
            //    $"C# test content - {DateTime.Now}",
            //    $"Topic for testing",
            //    "liveness-monitor-bot/notification").Result;

            //Console.WriteLine(
            //    $"Send private message attempt:\n" +
            //    $" success: {sendResponse.success}\n" +
            //    $" details: {sendResponse.details}\n" +
            //    $"      id: {sendResponse.messageId}");

            //if (sendResponse.success)
            //{
            //    System.Threading.Tasks.Task.Delay(1000).Wait();

            //    (bool success, string details) editResponse = client.Messages.TryEdit(
            //        sendResponse.messageId,
            //        $"Edited at: {DateTime.Now}").Result;

            //    Console.WriteLine(
            //        $"Edit message attempt:\n" +
            //        $" success: {editResponse.success}\n" +
            //        $" details: {editResponse.details}");
            //}

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
                // check for /secrets/.zuliprc
                string pathInSubdir = Path.Combine(currentDir, "secrets", "zuliprc");

                if (File.Exists(pathInSubdir))
                {
                    return pathInSubdir;
                }

                currentDir = Path.GetFullPath(Path.Combine(currentDir, ".."));

                if (currentDir == Path.GetPathRoot(currentDir))
                {
                    throw new DirectoryNotFoundException("Could not find zuliprc in path!");
                }

                filePath = Path.Combine(currentDir, "zuliprc");
            }

            return filePath;
        }

    }
}
