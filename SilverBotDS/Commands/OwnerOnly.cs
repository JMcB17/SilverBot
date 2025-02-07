﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Jering.Javascript.NodeJS;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SDBrowser;
using SilverBotDS.Objects;
using SilverBotDS.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace SilverBotDS.Commands
{
    [RequireOwner]
    [Hidden]
    internal class OwnerOnly : BaseCommandModule
    {
#pragma warning disable CA1822 // Mark members as static
        public DatabaseContext Database { private get; set; }
        public IBrowser Browser { private get; set; }
        public Config Config { private get; set; }
        public HttpClient HttpClient { private get; set; }

        [Command("repeat")]
        [Description("Repeats the message content")]
        public async Task Repeat(CommandContext ctx, [RemainingText][Description("The thing to repeat")] string e) => await ctx.RespondAsync(e);

        private readonly string[] _urls = { "https://silverdimond.tk", "https://vfl.gg", "https://github.com/silverdimond","https://cmpc.live","https://silverbot.cf","https://oscie.net" };

        [Command("riprandomframes")]
        [Description("less gooo baybae")]
        public async Task RipRandomFrames(CommandContext ctx, int times, string loc, string decoder="hevc", string encoder = "hevc")
        {
            var info = await FFmpeg.GetMediaInfo(loc).ConfigureAwait(false);
            await ctx.RespondAsync($"its {info.Duration.Humanize()} ({info.Duration}) long");

            using (RandomGenerator random = new())
            {
                string name = Path.GetFileName(loc);
                IVideoStream videoStream = info.VideoStreams.First()?.SetCodec(VideoCodec.png);
                for (int i = 0; i < times; i++)
                {
                    await FFmpeg.Conversions.New()
                    .AddStream(videoStream)
                    .ExtractNthFrame(random.Next(1, (int)(info.VideoStreams.First().Framerate * info.VideoStreams.First().Duration.TotalSeconds)), (number) => { return $"Extracts{Program.DirSlash}{name}{i}.png"; })
                    .UseHardwareAcceleration(HardwareAccelerator.auto, (VideoCodec)Enum.Parse(typeof(VideoCodec),decoder,true), (VideoCodec)Enum.Parse(typeof(VideoCodec), encoder, true))
                    .Start();
                }
            }
            await ctx.RespondAsync($"done?");
        }

        [Command("downloadffmpeg")]
        [Description("less gooo baybae")]
        public async Task Downloadffmpeg(CommandContext ctx)
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            await ctx.RespondAsync($"done?");
        }

        [Command("reloadcolors")]
        [Description("reloads the colors.json")]
        public async Task ReloadColors(CommandContext ctx)
        {
            if (!Config.ColorConfig)
            {
                await new DiscordMessageBuilder().WithEmbed(new DiscordEmbedBuilder().WithTitle("Reloading does nothing lololol").WithFooter("Requested by " + ctx.User.Username, ctx.User.GetAvatarUrl(ImageFormat.Png)).Build()).WithReply(ctx.Message.Id).SendAsync(ctx.Channel);
            }
            await ColorUtils.ReloadConfig();
            await new DiscordMessageBuilder().WithEmbed(new DiscordEmbedBuilder().WithTitle("Reloaded the colors").WithFooter("Requested by " + ctx.User.Username, ctx.User.GetAvatarUrl(ImageFormat.Png)).WithColor(await ColorUtils.GetSingleAsync()).Build()).WithReply(ctx.Message.Id).SendAsync(ctx.Channel);
        }

        [Command("stress")]
        [Description("less gooo baybae")]
        public Task Stress(CommandContext ctx)
        {
            foreach (var url in _urls)
            {
                _ = Webshot(ctx, url);
            }
            return Task.CompletedTask;
        }

        [Command("plot")]
        [Description("plot a thingy")]
        public async Task Plot(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
            await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("send x data"));
            var msg = await interactivity.WaitForMessageAsync((a) => { return a.Author.Id == ctx.User.Id; });
            if (msg.TimedOut)
            {
                await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("nvm your too slow"));
                return;
            }
            double[] xdata = Array.ConvertAll(msg.Result.Content.Split(' '), double.Parse);
            await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("send y data"));
            msg = await interactivity.WaitForMessageAsync((a) => { return a.Author.Id == ctx.User.Id; });
            if (msg.TimedOut)
            {
                await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("nvm your too slow"));
                return;
            }
            double[] ydata = Array.ConvertAll(msg.Result.Content.Split(' '), double.Parse);
            var plt = new ScottPlot.Plot(1920, 1080);
            plt.AddScatter(xdata, ydata);
            var bitmap = plt.Render();
            await using var outStream = new MemoryStream();
            bitmap.Save(outStream, System.Drawing.Imaging.ImageFormat.Png);
            outStream.Position = 0;
            await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("plotted that").WithFile("silverbotimage.png", outStream));
        }

        [Command("setupcategory")]
        [Description("Set up a category in the silverbot dev server")]
        [RequireBotPermissions(Permissions.ManageChannels | Permissions.ManageRoles)]
        public async Task Category(CommandContext ctx, [Description("The role to set up a category for")] DiscordRole
         role)
        {
            var name = Regex.Replace(role.Name, @"[^\w]", "");
            DiscordOverwriteBuilder[] builders = new[]
            {
            new DiscordOverwriteBuilder(ctx.Guild.CurrentMember)
            {
                Allowed = Permissions.All
            },
            new DiscordOverwriteBuilder(ctx.Guild.EveryoneRole)
            {
                Denied = Permissions.All
            },
            new DiscordOverwriteBuilder(role)
            {
                Allowed = Permissions.AccessChannels | Permissions.AddReactions | Permissions.AttachFiles | Permissions.ChangeNickname | Permissions.DeafenMembers | Permissions.EmbedLinks | Permissions.ManageChannels | Permissions.ManageMessages | Permissions.MoveMembers | Permissions.ReadMessageHistory | Permissions.SendMessages | Permissions.Speak | Permissions.Stream | Permissions.UseVoice | Permissions.UseVoiceDetection | Permissions.ManageRoles
            }};
            var category = await ctx.Guild.CreateChannelCategoryAsync(name, builders, $"Added by SilverBot as requested by {ctx.User.Username}");
            var channel = await ctx.Guild.CreateChannelAsync(name, ChannelType.Text, category, reason: $"Added by SilverBot as requested by {ctx.User.Username}");
            _ = await ctx.Guild.CreateChannelAsync(name, ChannelType.Voice, category, reason: $"Added by SilverBot as requested by {ctx.User.Username}");
            DiscordMessageBuilder discordMessage = new();
            discordMessage.Content = $"{ctx.User.Mention} there m8 that took some time to do";
            await channel.SendMessageAsync(discordMessage);
        }
        [Command("sudo"), Description("Executes a command as another user."), Hidden, RequireOwner]
        public async Task Sudo(CommandContext ctx, [Description("Member to execute as.")] DiscordMember member, [RemainingText, Description("Command text to execute.")] string command)
        {
            await ctx.TriggerTypingAsync();
            var cmd = ctx.CommandsNext.FindCommand(command, out var customArgs);
            var fakeContext = ctx.CommandsNext.CreateFakeContext(member, ctx.Channel, command, ctx.Prefix, cmd, customArgs);
            await ctx.CommandsNext.ExecuteCommandAsync(fakeContext);
        }

        [Command("setupcategory")]
        [Description("Set up a category in the silverbot dev server")]
        [RequireBotPermissions(Permissions.ManageChannels | Permissions.ManageRoles)]
        public async Task Category(CommandContext ctx, [Description("The person to set up a category for")] DiscordMember
             person)
        {
            var name = Regex.Replace(person.Username, @"[^\w]", "");
            DiscordOverwriteBuilder[] builders = new[]
            {
            new DiscordOverwriteBuilder(ctx.Guild.CurrentMember)
            {
                Allowed = Permissions.All
            },
            new DiscordOverwriteBuilder(ctx.Guild.EveryoneRole)
            {
                Denied = Permissions.All
            },
            new DiscordOverwriteBuilder(person)
            {
                Allowed = Permissions.AccessChannels | Permissions.AddReactions | Permissions.AttachFiles | Permissions.ChangeNickname | Permissions.DeafenMembers | Permissions.EmbedLinks | Permissions.ManageChannels | Permissions.ManageMessages | Permissions.MoveMembers | Permissions.ReadMessageHistory | Permissions.SendMessages | Permissions.Speak | Permissions.Stream | Permissions.UseVoice | Permissions.UseVoiceDetection | Permissions.ManageRoles
            }};
            var category = await ctx.Guild.CreateChannelCategoryAsync(name, builders, $"Added by SilverBot as requested by {ctx.User.Username}");
            var channel = await ctx.Guild.CreateChannelAsync(name, ChannelType.Text, category, reason: $"Added by SilverBot as requested by {ctx.User.Username}");
            _ = await ctx.Guild.CreateChannelAsync(name, ChannelType.Voice, category, reason: $"Added by SilverBot as requested by {ctx.User.Username}");
            await channel.SendMessageAsync($"{person.Mention} there m8 that took some time to do");
        }
        private readonly string[] imports = new[] { "System", "System.Collections.Generic", "System.Diagnostics", "System.IO", "System.IO.Compression", "System.Text", "System.Text.RegularExpressions", "System.Threading.Tasks", "System.Linq",  "Humanizer", "TimeSpanParserUtil", "Xabe.FFmpeg", "ScottPlot" };

        public static string RemoveCodeBraces(string str)
        {
            if (str.StartsWith("```csharp"))
            {
                str = str.Remove(0, 9);
            }
            if (str.StartsWith("```cs"))
            {
                str = str.Remove(0, 5);
            }
            if (str.StartsWith("```js"))
            {
                str = str.Remove(0, 5);
            }
            if (str.StartsWith("```javascript"))
            {
                str = str.Remove(0, 14);
            }
            if (str.StartsWith("```"))
            {
                str = str.Remove(0, 3);
            }
            if (str.StartsWith("``"))
            {
                str = str.Remove(0, 2);
            }
            if (str.StartsWith("`"))
            {
                str = str.Remove(0, 1);
            }
            if (str.EndsWith("```"))
            {
                str = str.Remove(str.Length - 3, 3);
            }
            if (str.EndsWith("``"))
            {
                str = str.Remove(str.Length - 2, 2);
            }
            if (str.EndsWith("`"))
            {
                str = str.Remove(str.Length - 1, 1);
            }
            return str;
        }

        public static async Task SendStringFileWithContent(CommandContext ctx, string title, string file, string filename = "message.txt")
        {
            await new DiscordMessageBuilder().WithContent(title).WithFile(filename, new MemoryStream(Encoding.UTF8.GetBytes(file))).WithAllowedMentions(Mentions.None).SendAsync(ctx.Channel);
        }
        public static async Task SendBestRepresentationAsync(object ob, CommandContext ctx)
        {
            try
            {
                string str = ob.ToString();
                if (ob is TimeSpan span)
                {
                    str = span.Humanize(20);
                }
                else if (ob is DateTime time)
                {
                    str = time.Humanize();
                }
                else if (ob is string @string)
                {
                    str = Formatter.BlockCode(@string);
                }
                else if (ob.GetType().IsSerializable || ob.GetType().IsArray || ob.GetType().IsEnum || ob.GetType().FullName == ob.ToString())
                {
                    str = JsonSerializer.Serialize(ob, options);
                    if (str.Length >= 2000)
                    {
                        await SendStringFileWithContent(ctx, ob.GetType().FullName, str, "eval.txt");
                        return;
                    }
                    else
                    {
                        str = Formatter.BlockCode(str, "json");
                    }
                }
                else
                {
                    str = Formatter.BlockCode(str);
                }
                if (ob.ToString().Length >= 2000)
                {
                    await SendStringFileWithContent(ctx, ob.GetType().FullName, str, "eval.txt");
                    return;
                }
                await new DiscordMessageBuilder().WithContent($"{ob.GetType().FullName} {str}").WithAllowedMentions(Mentions.None).SendAsync(ctx.Channel);
            }
            catch (Exception e)
            {
                Program.SendLog(e);
                await new DiscordMessageBuilder().WithContent($"Failed to parse `{ob.GetType().FullName}` as a string, using the generic ToString. {ob}").WithAllowedMentions(Mentions.None).SendAsync(ctx.Channel);
            }
        }

        public static readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Stolen idea from https://github.com/Voxel-Fox-Ltd/VoxelBotUtils/blob/master/voxelbotutils/cogs/owner_only.py#L172-L252
        /// </summary>
        [Command("evaluate")]
        [Description("EVALUATE SOME C# CODE")]
        [Aliases("eval", "ev")]
        public async Task Eval(CommandContext ctx, [RemainingText] string code)
        {
            TextWriter console = Console.Out;
            try
            {
                using var sw = new StringWriter();
                Console.SetOut(sw);
                var sw1 = Stopwatch.StartNew();
                var script = CSharpScript.Create(RemoveCodeBraces(code),
                ScriptOptions.Default.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location))).WithImports(imports), typeof(CodeEnv));
                var diag=script.Compile();
                sw1.Stop();
                if (diag.Length != 0)
                {
                   
                    if (diag.Humanize().Length > 1958)
                    {
                        await SendStringFileWithContent(ctx, "Compilation Diagnostics showed up:", diag.Humanize(), "diag.txt");
                    }
                    else
                    {
                        await new DiscordMessageBuilder().WithContent($"Compilation Diagnostics showed up: {Formatter.BlockCode(RemoveCodeBraces(diag.Humanize()), "cs")}").SendAsync(ctx.Channel);
                    }
                    var errcount = diag.LongCount(x => x.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
                    if (errcount!=0)
                    {
                        await new DiscordMessageBuilder().WithContent($"OH NO! After examining the diagnostics, I found {errcount} {(errcount==1?"error":"errors")}. That means if i try to run this it **WILL** fail. I will **NOT** attempt to run this code.").SendAsync(ctx.Channel);
                        return;
                    }
                }       
                await new DiscordMessageBuilder().WithContent($"Compiled the code in {sw1.Elapsed.Humanize(6)}").SendAsync(ctx.Channel);
                sw1.Start();
                var sw2 = Stopwatch.StartNew();
                var result = await script.RunAsync(new CodeEnv(ctx, Config));
                if (result.ReturnValue is not null)
                {
                    await SendBestRepresentationAsync(result.ReturnValue, ctx);
                }
                else
                {
                    await new DiscordMessageBuilder().WithContent($"The evaluated code returned a `null`. (null is a special marker that is used when something does not have a value, similar to **N**ot**AN**umber)").SendAsync(ctx.Channel);
                }
                if (!string.IsNullOrEmpty(sw.ToString()))
                {
                    if (sw.ToString().Length > 1978)
                    {
                        await SendStringFileWithContent(ctx, "Console Output:", sw.ToString(), "console.txt");
                    }
                    else
                    {
                        await new DiscordMessageBuilder().WithContent($"Console Output: {Formatter.BlockCode(sw.ToString())}").SendAsync(ctx.Channel);
                    }
                }
                sw.Close();
                Console.SetOut(console);
                await new DiscordMessageBuilder().WithContent($"Executed the code in {Formatter.Bold(sw2.Elapsed.Humanize(6))} excluding compile time, or {Formatter.Bold(sw1.Elapsed.Humanize(6))} including it.").SendAsync(ctx.Channel);
                sw1.Stop();
                sw2.Stop();
                result = null;
                script = null;
            }
            catch (CompilationErrorException e)
            {
                Console.SetOut(console);
                if (e.Diagnostics.Humanize().Length > 1958)
                {
                    await SendStringFileWithContent(ctx, "Compilation Error occurred:", e.Diagnostics.Humanize(), "error.txt");
                }
                else
                {
                    await new DiscordMessageBuilder().WithContent($"Compilation Error occurred: {Formatter.BlockCode(RemoveCodeBraces(e.Diagnostics.Humanize()), "cs")}").SendAsync(ctx.Channel);
                }
                throw;
            }
            catch (Exception)
            {
                Console.SetOut(console);
                throw;
            }
        }

        [Command("jsevaluate")]
        [Description("evaluates some js code")]
        [Aliases("jseval", "jsev")]
        public async Task JSEval(CommandContext ctx, [RemainingText] string code)
        {
            if (Config.UseNodeJs)
            {
                TextWriter console = Console.Out;
                try
                {
                    using var sw = new StringWriter();
                    Console.SetOut(sw);
                    DateTime start = DateTime.Now;
                    var script = await StaticNodeJSService.InvokeFromStringAsync<object>(RemoveCodeBraces(code));
                    DateTime aftercompile = DateTime.Now;
                    await new DiscordMessageBuilder().WithContent($"Ran the code in {(aftercompile - start).Humanize(6)}").SendAsync(ctx.Channel);
                    if (script is not null)
                    {
                        await SendBestRepresentationAsync(script, ctx);
                    }
                    else
                    {
                        await new DiscordMessageBuilder().WithContent($"Got a `null`").SendAsync(ctx.Channel);
                    }
                    if (!string.IsNullOrEmpty(sw.ToString()))
                    {
                        if (sw.ToString().Length > 1979)
                        {
                            await SendStringFileWithContent(ctx, "Console Output", sw.ToString(), "console.txt");
                        }
                        else
                        {
                            await new DiscordMessageBuilder().WithContent($"Console Output {Formatter.BlockCode(sw.ToString())}").SendAsync(ctx.Channel);
                        }
                    }
                    sw.Close();
                    Console.SetOut(console);
                    script = null;
                }
                catch (CompilationErrorException e)
                {
                    Console.SetOut(console);
                    if (e.Diagnostics.Humanize().Length > 1958)
                    {
                        await SendStringFileWithContent(ctx, "Compilation Error occurred:", e.Diagnostics.Humanize(), "error.txt");
                    }
                    else
                    {
                        await new DiscordMessageBuilder().WithContent($"Compilation Error occurred: {Formatter.BlockCode(RemoveCodeBraces(e.Diagnostics.Humanize()) ,"cs")}").SendAsync(ctx.Channel);
                    }
                    throw;
                }
                catch (Exception)
                {
                    Console.SetOut(console);
                    throw;
                }
            }
            else
            {
                await new DiscordMessageBuilder().WithContent("Nodejs is disabled in the config").SendAsync(ctx.Channel);
            }
        }

        [Command("sh")]
        public async Task RunConsole(CommandContext ctx, [RemainingText] string command)
        {
            Process main = new();
            main.StartInfo.FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "CMD.exe" : "/bin/bash";
            main.StartInfo.Arguments = $"{(Environment.OSVersion.Platform == PlatformID.Win32NT ? "/c" : "-c")} {command}";
            main.StartInfo.RedirectStandardOutput = true;
            main.StartInfo.RedirectStandardError = true;
            main.StartInfo.RedirectStandardInput = true;
            DiscordMessage msg = null;
            main.Start();
            ushort timesexited = 0;
            StringBuilder content = new();
            while (timesexited != 2)
            {
                while (main.StandardOutput.Peek() != -1)
                {
                    var readline = await main.StandardOutput.ReadToEndAsync() + await main.StandardError.ReadToEndAsync();
                    if (msg is null || msg.Content.Length + readline.Length + 7 >= 2000)
                    {
                        foreach (var part in StringUtils.SplitInParts(readline, 1991))
                        {
                            msg = await ctx.Channel.SendMessageAsync(Formatter.BlockCode(part));
                            content.Clear();
                            content.AppendLine(part);
                        }
                    }
                    else
                    {
                        await msg.ModifyAsync(Formatter.BlockCode($"{content}{readline}"));
                        content.AppendLine(readline);
                    }
                    await Task.Delay(2000);
                }
                if (main.HasExited)
                {
                    timesexited++;
                }
            }
        }

        [Command("runsql")]
        [Description("runs some sql")]
        public async Task Runsql(CommandContext ctx, [RemainingText] string sql)
        {
            var thing = await Database.RunSqlAsync(sql, Browser);
            if (thing.Item1 != null && thing.Item2 == null)
            {
                await new DiscordMessageBuilder().WithReply(ctx.Message.Id).WithContent(thing.Item1).SendAsync(ctx.Channel);
                return;
            }
            if (thing.Item1 == null && thing.Item2 != null)
            {
                var bob = new DiscordEmbedBuilder();
#pragma warning disable S1075 // URIs should not be hardcoded
                bob.WithImageUrl("attachment://html.png").WithFooter($"Requested by {ctx.User.Username}", ctx.User.GetAvatarUrl(ImageFormat.Png));
#pragma warning restore S1075 // URIs should not be hardcoded
                thing.Item2.Position = 0;
                await new DiscordMessageBuilder().WithEmbed(bob.Build()).WithFile("html.png", thing.Item2).SendAsync(ctx.Channel);
                thing.Item2.Dispose();
            }
        }

        private async Task<bool> IsBrowserNotSpecifed(CommandContext ctx)
        {
            bool a = Config.BrowserType == 0;
            if (a)
            {
                await new DiscordMessageBuilder().WithReply(ctx.Message.Id).WithContent("no browser specified").SendAsync(ctx.Channel);
            }
            return a;
        }

        [Command("webshot")]
        [Description("screenshots a webpage")]
        public async Task Webshot(CommandContext ctx, string html)
        {
            if (await IsBrowserNotSpecifed(ctx))
            {
                return;
            }
#pragma warning disable S1075 // URIs should not be hardcoded
            var bob = new DiscordEmbedBuilder().WithImageUrl("attachment://html.png").WithFooter($"Requested by {ctx.User.Username}", ctx.User.GetAvatarUrl(ImageFormat.Png)).WithColor(DiscordColor.Green);
#pragma warning restore S1075 // URIs should not be hardcoded
            using var e = await Browser.RenderUrlAsync(html);
            await new DiscordMessageBuilder().WithEmbed(bob.Build()).WithReply(ctx.Message.Id).WithFile("html.png", e).SendAsync(ctx.Channel);
        }

        public class Rootobject
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("author")]
            public string Author { get; set; }

            [JsonPropertyName("emotes")]
            public Emote[] Emotes { get; set; }
        }

        public class Emote
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class SourceFile
        {
            public string Name { get; set; }
            public string Extension { get; set; }
            public byte[] FileBytes { get; set; }
        }

        [Command("exportemotestoguilded")]
        [RequireGuild]
        public async Task ExportEmotesToGuilded(CommandContext ctx)
        {
            var emojiz = ctx.Guild.Emojis.Values;
            List<Emote> emotes = new();
            foreach (var emoji in emojiz)
            {
                emotes.Add(new Emote { Name = emoji.Name, Url = emoji.Url });
            }
            while(emotes.Count!=0)
            {
                await SendStringFileWithContent(ctx, "", JsonSerializer.Serialize(new Rootobject()
                {
                    Author = "SilverBot",
                    Name = $"{ctx.Guild.Name}'s emotes",
                    Emotes = emotes.Take(30).ToArray()
                }), "pack.json");
                if (emotes.Count >= 30)
                {
                    emotes.RemoveRange(0, 30);
                }
                else
                {
                    emotes.Clear();
                }
            }
            await new DiscordMessageBuilder().WithContent("https://support.guilded.gg/hc/en-us/articles/1500000398142").WithReply(ctx.Message.Id).SendAsync(ctx.Channel);
        }

        [Command("exportemotes")]
        [RequireGuild]
        public async Task DownloadEmotz(CommandContext ctx)
        {
            var emojiz = ctx.Guild.Emojis.Values;
            List<SourceFile> sourceFiles = new();
            foreach (var emoji in emojiz)
            {
                if (emoji.IsAnimated)
                {
                    sourceFiles.Add(new SourceFile { Name = emoji.Name, Extension = "gif", FileBytes = await HttpClient.GetByteArrayAsync(emoji.Url) });
                }
                else
                {
                    sourceFiles.Add(new SourceFile { Name = emoji.Name, Extension = "png", FileBytes = await HttpClient.GetByteArrayAsync(emoji.Url) });
                }
            }
            using MemoryStream memoryStream = new();
            using (ZipArchive zip = new(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (SourceFile f in sourceFiles)
                {
                    ZipArchiveEntry zipItem = zip.CreateEntry($"{f.Name}.{f.Extension}");
                    using MemoryStream originalFileMemoryStream = new(f.FileBytes);
                    using Stream entryStream = zipItem.Open();
                    originalFileMemoryStream.CopyTo(entryStream);
                }
            }
            memoryStream.Position = 0;
            await new DiscordMessageBuilder().WithReply(ctx.Message.Id).WithFile("emojis.zip", memoryStream).SendAsync(ctx.Channel);
        }

        [Command("addemotes")]
        [Description("testing shiz")]
        [RequireGuild]
        [RequirePermissions(Permissions.ManageEmojis)]
        public async Task Addemotez(CommandContext ctx)
        {
            var lang = await Language.GetLanguageFromCtxAsync(ctx);
            if (ctx.Message.Attachments.Count == 0)
            {
                await ctx.RespondAsync(lang.NoImageGeneric);
                return;
            }
            if (ctx.Message.Attachments.Count > 1)
            {
                await ctx.RespondAsync(lang.MoreThanOneImageGeneric);
                return;
            }
            if (FileUtils.GetFileExtensionFromUrl(ctx.Message.Attachments[0].Url) != ".zip")
            {
                await ctx.RespondAsync("please use a zip");
                return;
            }
            var client = HttpClient;
            var ziploc = $"{Environment.CurrentDirectory}{Program.DirSlash}temp.zip";
            var rm = await client.GetAsync(ctx.Message.Attachments[0].Url);
            await using (var fs = new FileStream(
    ziploc,
    FileMode.CreateNew))
            {
                await rm.Content.CopyToAsync(fs);
            }
            var foldername = ($"{Environment.CurrentDirectory}{Program.DirSlash}temp");
            if (!Directory.Exists(foldername))
            {
                Directory.CreateDirectory(foldername);
            }
            else if (Directory.GetFiles(foldername).Length != 0)
            {
                Directory.Delete(foldername, true);
                Directory.CreateDirectory(foldername);
            }
            ZipFile.ExtractToDirectory(ziploc, foldername);
            StringBuilder status = new();
            foreach (var file in Directory.GetFiles(foldername))
            {
                await using FileStream fileStream = new(file, FileMode.Open);
                await using var stream = new MemoryStream();
                fileStream.Position = 0;
                await fileStream.CopyToAsync(stream);
                if (stream.Length > 256 * 1000)
                {
                    status.Append(Path.GetFileName(file));
                    status.Append("\t " + StringUtils.BoolToEmoteString(false) + " Bigger than 256kb");
                }
                else
                {
                    var emote = await ctx.Guild.CreateEmojiAsync(name: Path.GetFileNameWithoutExtension(file),
                        image: stream, reason: $"Added by SilverBot as requested by {ctx.User.Username}");
                    status.Append("\t " + emote + ' ' + StringUtils.BoolToEmoteString(true));
                }
            }
            await ctx.RespondAsync(status.ToString());
            Directory.Delete(foldername, true);
            File.Delete(ziploc);
        }

        [Command("guilds")]
        public async Task Guilds(CommandContext ctx)
        {
            StringBuilder bob = new();
            foreach (var guild in ctx.Client.Guilds.Values)
            {
                bob.AppendLine($"{guild.Name} | {guild.Owner.DisplayName} | {guild.MemberCount} | {guild.Id}");
            }
            await ctx.RespondAsync(bob.ToString());
        }

        [Command("shutdown")]
        [Description("kill the bot")]
        public async Task Reloadsplashes(CommandContext ctx)
        {
            await ctx.RespondAsync("bye");
            Environment.Exit(469);
        }

        [Command("screenshothtml")]
        [Description("UHHHHHHHHHHHHH its a secret")]
        public async Task Html(CommandContext ctx, string html)
        {
            if (await IsBrowserNotSpecifed(ctx))
            {
                return;
            }
            var bob = new DiscordEmbedBuilder();
#pragma warning disable S1075 // URIs should not be hardcoded
            bob.WithImageUrl("attachment://html.png").WithFooter("Requested by " + ctx.User.Username, ctx.User.GetAvatarUrl(ImageFormat.Png));
#pragma warning restore S1075 // URIs should not be hardcoded
            using var e = await Browser.RenderHtmlAsync(html);

            e.Position = 0;
            await new DiscordMessageBuilder().WithEmbed(bob.Build()).WithFile("html.png", e).SendAsync(ctx.Channel);
        }
    }
}