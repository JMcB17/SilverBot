﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace SilverBotDS.Converters
{
    public class RequireDJAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help) => (help && ctx.Channel.IsPrivate) || ctx.Member.Roles.Any(e => e.CheckPermission(DSharpPlus.Permissions.ManageChannels) == DSharpPlus.PermissionLevel.Allowed || e.Name.ToLower().Contains("dj")) || (ctx.Member.VoiceState is not null && ctx.Member.VoiceState.Channel is not null && ctx.Member.VoiceState.Channel.Users is not null && ctx.Member.VoiceState.Channel.Users.LongCount(x => !x.IsBot) == 1)
                ? Task.FromResult(true)
                : Task.FromResult(false);
    }
}