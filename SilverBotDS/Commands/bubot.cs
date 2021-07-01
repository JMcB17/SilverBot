﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SilverBotDS.Exceptions;
using SilverBotDS.Objects;
using SilverBotDS.Utils;
namespace SilverBotDS.Commands
{
    internal class Bubot : BaseCommandModule
    {
        private readonly Font BibiFont = new(SystemFonts.Find("Arial"), 30, FontStyle.Bold);
        private readonly int BibiPictureCount = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("SilverBotDS.Templates.Bibi.") && x.EndsWith(".png")).Count();

        [Command("silveryeet")]
        [Description("Sends SilverYeet.gif")]
        public async Task Silveryeet(CommandContext ctx) => await new DiscordMessageBuilder().WithContent("https://cdn.discordapp.com/attachments/751246248102592567/823475242822533120/SilverYeet.gif").WithReply(ctx.Message.Id).WithAllowedMentions(Mentions.None).SendAsync(ctx.Channel);
     
        [Command("WeWillFockYou")]
        [Description("Gives a Youtube link for the legendary We Will Fock You video.")]
        public async Task WeWillFockYou(CommandContext ctx) => await new DiscordMessageBuilder().WithContent("https://youtu.be/lLN3caSQI1w").WithReply(ctx.Message.Id).WithAllowedMentions(Mentions.None).SendAsync(ctx.Channel);

        [Command("bibi")]
        [Description("Makes a image with the great cat Bibi.")]
        [Cooldown(1, 2, CooldownBucketType.User)]
        public async Task Bibi(CommandContext ctx, [RemainingText][Description("Bibi is")] string input)
        {
            await ctx.TriggerTypingAsync();
            input = "bibi is " + input;
            int randomnumber;
            using (var random = new RandomGenerator())
            {
                randomnumber = random.Next(1, BibiPictureCount);
            }
            using Image picture = await Image.LoadAsync(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    $"SilverBotDS.Templates.Bibi.{randomnumber}.png"
                )
                ??
                throw new TemplateReturningNullException(
                    $"SilverBotDS.Templates.Bibi.{randomnumber}.png"
                )
            );
            float size = BibiFont.Size;
            while (TextMeasurer.Measure(input, new RendererOptions(new Font(BibiFont.Family, size, FontStyle.Bold))).Width > picture.Width)
            {
                size -= 0.05f;
            }
            picture.Mutate(
                x => x.DrawText(
                    input, new Font(BibiFont.Family, size, FontStyle.Bold), randomnumber is 10 or 9 ? Color.Gray : Color.White, new PointF(4, 230)
                )
            );
            await using var outStream = new MemoryStream();
            await picture.SaveAsPngAsync(outStream);
            outStream.Position = 0;
            randomnumber = 0;
            await ImageModule.SendImageStream(ctx, outStream, content: input);
        }
        
    
    }
    [Group("bibiLibrary")]
    internal class BibiLib : BaseCommandModule
    {
        private readonly int BibiCutoutPictureCount = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("SilverBotDS.Templates.BibiLibCutout.") && x.EndsWith(".png")).Count();
        private readonly int BibiFullPictureCount = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("SilverBotDS.Templates.BibiLibCutout.") && x.EndsWith(".png")).Count();
        [GroupCommand]
        [Description("Access the great cat bibi library.")]
        public async Task bibiLibrary(CommandContext ctx, [RemainingText] string term)
        {
            var lang = await Language.GetLanguageFromCtxAsync(ctx);
            var page = 1;
            var b = new DiscordEmbedBuilder();

            var imgurl = $"https://github.com/thesilvercraft/SilverBot/blob/master/SilverBotDS/Templates/Bibi/{page}.png?raw=true";
            b.WithDescription($"text about the great cat Bibi picture here : {imgurl}  {string.Format(lang.PageGif, 1, BibiCutoutPictureCount)}").WithFooter(lang.RequestedBy + ctx.User.Username, ctx.User.GetAvatarUrl(ImageFormat.Png)).WithImageUrl(imgurl).WithColor(color: await ColorUtils.GetSingleAsync());
#pragma warning restore S1075 // URLs should not be hardpissed
            await WaitForNextMessage(ctx, await new DiscordMessageBuilder().WithReply(ctx.Message.Id).WithEmbed(b.Build()).AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "nextgif", lang.PageGifButtonText)).SendAsync(ctx.Channel), ctx.Client.GetInteractivity(), lang, page, b);
        }
        private async Task WaitForNextMessage(CommandContext ctx, DiscordMessage oldmessage, InteractivityExtension interactivity, Language lang, int page, DiscordEmbedBuilder b = null)
        {
            b ??= new DiscordEmbedBuilder();
            var msg = await oldmessage.WaitForButtonAsync(ctx.User, TimeSpan.FromSeconds(300));
            var imgurl = $"https://github.com/thesilvercraft/SilverBot/blob/master/SilverBotDS/Templates/Bibi/{page}.png?raw=true";
            if (msg.Result != null)
            {
                page++;
                if (page >= BibiCutoutPictureCount)
                {
                    page = 0;
                }
                b.WithDescription($" : {imgurl} {string.Format(lang.PageGif, page + 1, BibiCutoutPictureCount)}").WithImageUrl(imgurl).WithColor(color: await ColorUtils.GetSingleAsync());
                await msg.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(new DiscordMessageBuilder().WithEmbed(b).AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "nextgif", lang.PageGifButtonText))));
                await WaitForNextMessage(ctx, oldmessage, interactivity, lang, page, b);
            }
            else
            {
                await oldmessage.ModifyAsync(new DiscordMessageBuilder().WithEmbed(b).WithContent(lang.PeriodExpired).AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "nextgif", lang.PageGifButtonText, true)));
            }
        }
    }
    }