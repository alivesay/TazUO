using System;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using Discord.Sdk;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls;

public class DiscordMessageControl : Control
{
    public DiscordMessageControl(MessageHandle msg, int width)
    {
        Width = width;
        CanMove = true;
        
        
        DateTime time = DateTimeOffset.FromUnixTimeMilliseconds((long)msg.SentTimestamp()).UtcDateTime.ToLocalTime();

        var content = msg.Content();

        if (string.IsNullOrEmpty(content))
        {
            var adtl = msg.AdditionalContent();

            if (adtl == null)
            {
                Dispose();
                return;
            }

            if (adtl.Type() == AdditionalContentType.Attachment || adtl.Type() == AdditionalContentType.Embed)
            {
                content = "[- User sent an attachment, unable to view here. -]";
            }
        }

        var name = TextBox.GetOne($"[{time.ToShortTimeString()}] {msg.Author()?.DisplayName()}: ", TrueTypeLoader.EMBEDDED_FONT, 20f, DiscordManager.GetUserhue(msg.AuthorId()), TextBox.RTLOptions.Default());
        var message = TextBox.GetOne(content, TrueTypeLoader.EMBEDDED_FONT, 20f, Color.White, TextBox.RTLOptions.Default(width - name.Width));
        message.X = name.Width;
        
        Add(name);
        Add(message);

        Height = message.Height;
    }
}