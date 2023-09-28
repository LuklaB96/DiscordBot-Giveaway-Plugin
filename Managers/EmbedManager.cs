using Discord;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Giveaway.Managers
{
    public class EmbedManager
    {
        private string title;
        private string prize;
        private TimestampTag endsAt;
        private string authorId;
        private string footerId;
        private int maxWinners;
        private int entries;
        private bool open;

        public EmbedManager(bool open = true)
        {
            title = string.Empty;
            prize = string.Empty;
            endsAt = TimestampTag.FromDateTime(DateTime.Now);
            authorId = string.Empty;
            footerId = string.Empty;
            maxWinners = 0;
            entries = 0;
            this.open = open;
        }
        public Embed Build()
        {
            if (open)
            {
                var embed = new EmbedBuilder();
                embed.WithTitle($"{title}");
                embed.WithDescription($"Prize: {prize}");
                embed.AddField("\u200b", $"Entries: {entries}");
                embed.AddField("\u200b", $"Ends at: {endsAt} \r\n Max winners: {maxWinners} \r\n Host: <@{authorId}>", inline: true);
                embed.WithFooter($"ID: {footerId}");
                embed.WithCurrentTimestamp();
                return embed.Build();
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithTitle($"{title}");
                embed.WithDescription($"Prize: {prize}");
                embed.AddField("\u200b", $"Entries: {entries}");
                embed.AddField("\u200b", $"Ends at: {endsAt} \r\n Max winners: {maxWinners} \r\n Host: <@{authorId}>", inline: true);
                embed.AddField("\u200b", $":lock: Giveaway closed! :lock:");
                embed.WithFooter($"ID: {footerId}");
                embed.WithCurrentTimestamp();
                return embed.Build();
            }
        }
        public EmbedManager WithTitle(string title)
        {
            this.title = title;
            return this;
        }
        public EmbedManager Prize(string prize)
        {
            this.prize = prize;
            return this;
        }
        public EmbedManager Enteries(int entries)
        {
            this.entries = entries;
            return this;
        }
        public EmbedManager EndsAt(TimestampTag endsAt)
        {
            this.endsAt = endsAt;
            return this;
        }
        public EmbedManager AuthorId(string authorId)
        {
            this.authorId = authorId;
            return this;
        }
        public EmbedManager GiveawayId(string id)
        {
            footerId = id;
            return this;
        }
        public EmbedManager MaxWinners(int maxWinners)
        {
            this.maxWinners = maxWinners;
            return this;
        }

    }
}
