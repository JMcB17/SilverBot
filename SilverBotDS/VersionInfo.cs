﻿using CodenameGenerator;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using WebClient = SilverBotDS.Objects.WebClient;

namespace SilverBotDS
{
    internal static class VersionInfo
    {
        public const string VNumber = ThisAssembly.Git.Commit + "-" + ThisAssembly.Git.Branch + "-" + ThisAssembly.Git.CommitDate;

        private static void LogLine(string line)
        {
            Console.WriteLine($"[VersionInfo]: {line}");
        }

        public static async void Checkforupdates()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                LogLine("Running on " + Environment.OSVersion.VersionString);
                Console.ResetColor();
                var client = WebClient.Get();
                LogLine("Getting latest version info from silverdimond.tk");
                var rm = await client.GetAsync("https://silverdimond.tk/silvercraftbot/version-info.txt");
                var content = await rm.Content.ReadAsStringAsync();
                var strings = content.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                var uptodate = true;
                if (strings.Length != 3)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    LogLine($"Oh no, silverdimond.tk returned more than three lines, it returned {strings.Length} lines, this probably means its using a different format or someone made an error.");
                    Console.ResetColor();
                }
                if (strings[0] != VNumber)
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    LogLine($"You are running {VNumber} while the latest version is {strings[0]}, consider updating.");
                    Console.ResetColor();
                    uptodate = false;
                }
                if (uptodate)
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    LogLine($"You are currently running {VNumber}, which is the latest version (according to silverdimond.tk)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    LogLine($"You should go to {strings[2]} to download a new version of SilverBot");
                    Console.ResetColor();
                }
#if DEBUG
                if (uptodate || (Environment.UserDomainName != "DESKTOP-QK1H9BG")) return;
                await using (var sw = new StreamWriter("version-info.txt"))
                {
                    await sw.WriteLineAsync(VNumber);
                    var generator = new Generator
                    {
                        Separator = "-"
                    };
                    await sw.WriteLineAsync($"codename {generator.Generate()}");
                    await sw.WriteLineAsync(ThisAssembly.Git.RepositoryUrl);
                }
                Process.Start("notepad", "version-info.txt");

#endif
            }
            catch (WebException)
            {
                LogLine("We got a WebException, canceling version check.");
            }
        }
    }
}