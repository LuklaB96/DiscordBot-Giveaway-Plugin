using Discord;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Giveaway.Managers
{
    public class EmbedManager
    {
        private string _title;
        private string _prize;
        private TimestampTag _endsAt;
        private string _authorId;
        private string _footerId;
        private int _maxWinners;
        private int _entries;
        private bool _open;

        public EmbedManager(bool open = true)
        {
            _title = string.Empty;
            _prize = string.Empty;
            _endsAt = TimestampTag.FromDateTime(DateTime.Now);
            _authorId = string.Empty;
            _footerId = string.Empty;
            _maxWinners = 0;
            _entries = 0;
            _open = open;
        }
        public Embed Build()
        {
            if (_open)
            {
                var embed = new EmbedBuilder();
                embed.WithTitle($"{_title}");
                embed.WithDescription($"Prize: {_prize}");
                embed.AddField("\u200b", $"Entries: {_entries}");
                embed.AddField("\u200b", $"Ends at: {_endsAt} \r\n Max winners: {_maxWinners} \r\n Host: <@{_authorId}>", inline: true);
                embed.WithFooter($"ID: {_footerId}");
                embed.WithCurrentTimestamp();
                return embed.Build();
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithTitle($"{_title}");
                embed.WithDescription($"Prize: {_prize}");
                embed.AddField("\u200b", $"Entries: {_entries}");
                embed.AddField("\u200b", $"Ends at: {_endsAt} \r\n Max winners: {_maxWinners} \r\n Host: <@{_authorId}>", inline: true);
                embed.AddField("\u200b", $":lock: Giveaway closed! :lock:");
                embed.WithFooter($"ID: {_footerId}");
                embed.WithCurrentTimestamp();
                return embed.Build();
            }
        }
        public EmbedManager WithTitle(string title)
        {
            this._title = title;
            return this;
        }
        public EmbedManager Prize(string prize)
        {
            _prize = prize;
            return this;
        }
        public EmbedManager Enteries(int entries)
        {
            _entries = entries;
            return this;
        }
        public EmbedManager EndsAt(TimestampTag endsAt)
        {
            _endsAt = endsAt;
            return this;
        }
        public EmbedManager AuthorId(string authorId)
        {
            _authorId = authorId;
            return this;
        }
        public EmbedManager GiveawayId(string id)
        {
            _footerId = id;
            return this;
        }
        public EmbedManager MaxWinners(int maxWinners)
        {
            _maxWinners = maxWinners;
            return this;
        }

    }
}
