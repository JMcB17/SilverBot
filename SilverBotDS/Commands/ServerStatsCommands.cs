﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SilverBotDS.Commands
{
    internal class ServerStatsCommands : BaseCommandModule
    {
        private readonly Regex _emote = new("<(a)?:(?<name>.+?):(?<id>.+?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        [Command("emoteanalyse")]
        [Description("analyse emote usage in a specified channel")]
        [Cooldown(1, 60 * 60, CooldownBucketType.Guild)]
        public async Task EmoteAnalytics(CommandContext ctx, DiscordChannel channel, int limit = 10000)
        {
            await new DiscordMessageBuilder().WithReply(ctx.Message.Id)
                                             .WithContent($"starting emoji getter bot dot exe 69696969696969969696\nDownloading at most {limit} messages which will take around {new TimeSpan(0, 0, limit / 100).Humanize(2)} cause of discord ratelimit")
                                             .SendAsync(ctx.Channel);
            DateTime start = DateTime.Now;
            var messages = await channel.GetMessagesAsync(limit);
            await new DiscordMessageBuilder().WithReply(ctx.Message.Id)
                                            .WithContent($"Downloaded {messages.Count} while expecting {limit}, estimated time was: {new TimeSpan(0, 0, limit / 100).Humanize(2)} actual time was {(DateTime.Now - start).Humanize(2)} expected time if provided correct limit would be {new TimeSpan(0, 0, messages.Count / 100).Humanize(2)}\nWell anyways processing messages")
                                            .SendAsync(ctx.Channel);
            DateTime startproc = DateTime.Now;
            StringBuilder bob = new("Name,Id,Timestamp\n");
            ulong e = 0;
            foreach (var message in messages)
            {
                foreach (var part in message.Content.Split(' '))
                {
                    var m = _emote.Matches(part);
                    if (m.Count != 0)
                    {
                        foreach (Match match in m.ToArray())
                        {
                            bob.AppendLine($"{match.Groups["name"].Value},{Convert.ToUInt64(match.Groups["id"].Value)},{ (DateTimeOffset)(message.EditedTimestamp != null ? message.EditedTimestamp : message.Timestamp):yyyy-MM-dd HH:mm:ss}");
                        }
                    }
                }
                e++;
            }
            await OwnerOnly.SendStringFileWithContent(ctx, $"i went through {messages.Count}messages in {(DateTime.Now - start).Humanize(2)}(including download) {(DateTime.Now - startproc).Humanize(2)}(excluding download)\nEmote usage data:", bob.ToString(), "emotes.csv");
            bob.Clear();
            GC.Collect();
        }
    }
}