using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using DiscordPluginAPI;
using DiscordPluginAPI.Enums;
using DiscordPluginAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordPluginAPI.Helpers;
using Giveaway.Managers;

namespace Giveaway
{
    public enum GiveawayStatus
    {
        Open,
        Closed,
        UserAddFail,
        UserRemoveFail,
        UserAdded,
        UserRemoved,
        UserExist
    }
    public enum EmbedOptions
    {
        increase,
        decrease,
        CloseGiveaway,
        OpenGiveaway,
    }
    public class Giveaway : IPlugin, IPluginCommands, IPluginComponents
    {
        public ILogger Logger { get; set; }
        public EmbedOptions[] defaultOptions = { EmbedOptions.increase };
        private static List<string> activeGiveaways;
        public CommandType commandType => CommandType.Slash;
        public bool ConfigRequired => false;
        public bool DatabaseRequired => false;
        public SlashCommandBuilder[] slashCommandBuilder => new SlashCommandBuilder[1] { new SlashCommandBuilder()
                .WithName("giveaway")
                .WithDescription("Create and manage your giveaways!")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("create")
                    .WithDescription("Create new giveaway!")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("title", ApplicationCommandOptionType.String, "Title of your giveaway", isRequired: true)
                    .AddOption("prize", ApplicationCommandOptionType.String, "Giveaway prize", isRequired: true)
                    .AddOption("winners", ApplicationCommandOptionType.Integer, "Maximum winners, Default: 1", isRequired: false)
                    .AddOption("ends", ApplicationCommandOptionType.Integer, "After how many days giveaway will automatically end? Accepts only number 1,2,3... Default: 1", isRequired: false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("close")
                    .WithDescription("Close existing giveaway!")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("id", ApplicationCommandOptionType.String, "Insert id of the existing giveaway, it can be found at the embed footer.", isRequired: true)
                    )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("open")
                    .WithDescription("Open existing giveaway again!")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("id", ApplicationCommandOptionType.String, "Insert id of the existing giveaway, it can be found at the embed footer.", isRequired: true)
                    )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("roll")
                    .WithDescription("Roll the winners!")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("id", ApplicationCommandOptionType.String, "Insert id of the existing giveaway, it can be found at the embed footer.", isRequired: true)
                    )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("list")
                    .WithDescription("Show all active giveaways on this server.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                ) 
        };

        public Dictionary<string, string> DatabaseTableProperties => new Dictionary<string, string>()
        {
            ["list"] = "id TEXT PRIMARY KEY, guild_id TEXT, channel_id TEXT, message_id TEXT, message_owner TEXT, title TEXT, description TEXT, max_winners INTEGER, winner TEXT, entries INTEGER DEFAULT 0 NOT NULL, ends TEXT DEFAULT 1 NOT NULL, ended INTEGER DEFAULT 0 NOT NULL, closed INTEGER DEFAULT 0 NOT NULL, created_at TEXT",
            ["users"] = "id INTEGER PRIMARY KEY, giveaway_id TEXT, user_id TEXT"
        };

        public Config Config { get; set; }

        public List<string> PrefixCommands { get; set; }

        public string Name { get; set; }
        public List<SocketGuild> guilds { get; set; }
        public DiscordSocketClient Client { get; set; }

        IDatabase Database;

        public async Task<Task> Initalize(IDatabase database = null, ILogger logger = null)
        {
            Name = "Giveaway24";
            Database = database;
            Logger = logger;
            return Task.CompletedTask;
        }

        public async Task<object> ExecuteSlashCommand(SocketSlashCommand command = null)
        {
            Dictionary<string, string> args = command.Data.Options.First().Options.ToDictionary(x => x.Name, x => x.Value.ToString());
            var sub = command.Data.Options.First().Name;
            RestInteractionMessage msg = null;
            switch (sub)
            {

                case "create":
                    msg = await Create(command, args);
                    break;
                case "close":
                    await Close(command, args);
                    break;
                case "open":
                    await Open(command, args);
                    break;
                case "roll":
                    await Roll(command, args);
                    break;
                case "list":
                    await List(command, args);
                    break;
                default:
                    return null;
            }
            return msg;
        }
        public Task<object> ExecutePrefixCommand(SocketMessage message)
        {
            throw new NotImplementedException();
        }
        public Embed CreateGiveawayEmbed(Dictionary<string, string> args)
        {
            double days = 1;

            string maxWinners = args.ContainsKey("winners") == true ? args["winners"] : "1";
            maxWinners = Int32.Parse(maxWinners) <= 0 ? "1" : maxWinners;

            string ends = args.ContainsKey("ends") == true ? args["ends"] : "1";

            double.TryParse(ends, out days);

            var time = TimestampTag.FromDateTime(DateTime.Now.AddDays(days));

            if (args.Count == 0 || args.Count < 3) return null;

            EmbedManager giveawayEmbedNew = new EmbedManager()
                .WithTitle(args["title"])
                .Prize(args["prize"])
                .Enteries(0).EndsAt(time)
                .MaxWinners(Int32.Parse(maxWinners))
                .AuthorId(args["user_id"])
                .GiveawayId(args["guid"]);

            return giveawayEmbedNew.Build();
        }

        public async Task<RestInteractionMessage> Create(SocketSlashCommand command, Dictionary<string, string> args)
        {
            string maxWinners = args.ContainsKey("winners") == true ? args["winners"] : "1";
            maxWinners = int.Parse(maxWinners) <= 0 ? "1" : maxWinners;
            string ends = args.ContainsKey("ends") == true ? args["ends"] : "1";
            args.Add("user_id", command.User.Id.ToString());
            string guid = Uuid.Create(Name);
            args.Add("guid", guid);
            string guildId = command.GuildId.ToString();

            var embed = CreateGiveawayEmbed(args);

            var button = new ComponentBuilder().WithButton("Join", guid, ButtonStyle.Primary, new Emoji("🎉"));

            // Send the embed to the channel
            await command.RespondAsync("", null, false, false, null, button.Build(), embed);

            var msg = await command.GetOriginalResponseAsync();
            var messageId = msg.Id.ToString();
            var channelId = msg.Channel.Id.ToString();
            var author = command.User.Id.ToString();
            DateTime date = DateTime.Now;
            date = date.AddDays(double.Parse(ends));
            string endDate = date.ToString("yyyy-MM-dd HH:mm:ss");
            if (command.HasResponded)
            {
                QueryParametersBuilder parameters = new QueryParametersBuilder();
                parameters.Add("@Id", guid);
                parameters.Add("@GuildId", guildId);
                parameters.Add("@ChannelId", channelId);
                parameters.Add("@MessageId", messageId);
                parameters.Add("@Author", author);
                parameters.Add("@Title", args["title"]);
                parameters.Add("@Description", args["prize"]);
                parameters.Add("@MaxWinners", maxWinners);
                parameters.Add("@Entries", "0");
                parameters.Add("@Ends", endDate);
                parameters.Add("@Ended", "0");
                parameters.Add("@Closed", "0");

                const string query = "INSERT INTO #list (id, guild_id, channel_id, message_id, message_owner, title, description, max_winners, entries, ends, ended, closed, created_at) VALUES (@Id, @GuildId, @ChannelId, @MessageId, @Author, @Title, @Description, @MaxWinners, @Entries, @Ends, @Ended, @Closed, datetime('now', 'localtime'))";
                await Database.InsertQueryAsync(query, parameters, Config);
                await UpdateList();
            }
            return msg;
        }

        public async Task Close(SocketSlashCommand command, Dictionary<string, string> args)
        {
            const string updateQuery = "UPDATE #list SET closed = @Closed WHERE id = @Id";
            const string selectQuery = "SELECT message_id, id, title, description, message_owner, max_winners, entries, ends FROM #list WHERE id = @Id";
            const string closed = "1";

            string id = args["id"];

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@Id", id);
            parameters.Add("@Closed",closed);

            int result = await Database.UpdateQueryAsync(updateQuery, parameters, Config);
            var data = await Database.SelectQueryAsync(selectQuery, parameters, Config);

            if (result > 0)
            {
                string giveawayId = data[1];
                string title = data[2];
                string description = data[3];
                string authorId = data[4];
                string maxWinners = data[5];
                string entries = data[6];
                var endsAt = DateTime.Parse(data[7]);

                EmbedManager giveawayEmbedClose = new EmbedManager(open: false)
                    .WithTitle(title)
                    .Prize(description)
                    .Enteries(Int32.Parse(entries))
                    .EndsAt(TimestampTag.FromDateTime(endsAt))
                    .MaxWinners(Int32.Parse(maxWinners))
                    .AuthorId(authorId)
                    .GiveawayId(giveawayId);

                await command.RespondAsync("Giveaway closed!", ephemeral: true);
                var channel = command.Channel as ITextChannel;
                var msg = await channel.GetMessageAsync(ulong.Parse(data[0]));

                await channel.ModifyMessageAsync(msg.Id, x => { x.Embed = giveawayEmbedClose.Build(); });
                return;
            }

            await command.RespondAsync("Wrong giveaway id or already closed!", ephemeral: true);
        }
        public async Task<EmbedBuilder> EditGiveawayEmbed(EmbedBuilder embed, string id = null, EmbedOptions[] options = null)
        {
            options ??= defaultOptions;
            int entries = 0;

            const string selectQuery = "SELECT entries FROM #list where id = @Id";

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@Id", id);

            var items = await Database.SelectQueryAsync(selectQuery, parameters, Config);
            if (items.Count > 0)
                entries = Convert.ToInt32(items[0]);

            foreach (var option in options)
            {
                switch (option)
                {
                    case EmbedOptions.increase:
                        entries++;
                        break;
                    case EmbedOptions.decrease:
                        if (entries > 0) entries--;
                        break;
                    case EmbedOptions.CloseGiveaway:
                        break;
                    case EmbedOptions.OpenGiveaway:
                        var lastField = embed.Fields.LastOrDefault();
                        if (lastField.Value.ToString().ToLower().Contains("closed"))
                        {
                            var field = embed.Fields.LastOrDefault();
                            embed.Fields.Remove(field);
                            return embed;
                        }
                        else
                        {
                            return null;
                        }
                }
            }


            embed.Fields[0].Value = "Entries: " + entries;

            parameters.Add("@Entries", entries.ToString());

            const string updateQuery = "UPDATE #list SET entries = @Entries WHERE id = @Id";
            await Database.UpdateQueryAsync(updateQuery, parameters, Config);
            return embed;
        }
        public async Task Open(SocketSlashCommand command, Dictionary<string, string> args)
        {
            const string selectQuery = "SELECT closed,ended,message_id,ends FROM #list WHERE id = @Id";
            const string updateQuery = "UPDATE #list SET closed = @Closed, ends = @Ends WHERE id = @Id";
            const string closed = "0";

            string id = args["id"];

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@Id", id);
            parameters.Add("@Closed", closed);

            var data = await Database.SelectQueryAsync(selectQuery, parameters, Config);
            try
            {
                if (data.Count > 0 && data[0] == "1" && data[1] == "0")
                {
                    var oldDateString = data[3];

                    DateTime oldDate = DateTime.Parse(oldDateString);
                    DateTime newDate = DateTime.Now;
                    if (DateTime.Compare(oldDate,newDate) < 0)
                        newDate = newDate.AddDays(1);
                    else
                        newDate = oldDate;

                    parameters.Add("@Ends", newDate.ToString("yyyy-MM-dd HH:mm:ss"));

                    await Database.UpdateQueryAsync(updateQuery, parameters, Config);
                    await command.RespondAsync($"Giveaway is now open! And will be closed again at {newDate.ToString("yyyy-MM-dd HH:mm:ss")}", ephemeral: true);

                    var channel = command.Channel as ITextChannel;
                    var msg = await channel.GetMessageAsync(ulong.Parse(data[2]));
                    var embed = msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                    var newEmbed = await EditGiveawayEmbed(embed, options: new EmbedOptions[] { EmbedOptions.OpenGiveaway });

                    await channel.ModifyMessageAsync(msg.Id, x => { x.Embed = newEmbed.Build(); });

                    return;
                }
            } catch(Exception ex) { Console.WriteLine(ex); }


            await command.RespondAsync("Wrong giveaway id or already opened/ended!", ephemeral: true);
        }
        public async Task Roll(SocketSlashCommand command, Dictionary<string, string> args)
        {
            try
            {
                string id = args["id"];

                const string selectGiveawayDataQuery = "SELECT max_winners,ended,message_id,closed FROM #list WHERE id = @Id";
                const string selectGiveawayUsersQuery = "SELECT user_id FROM #users WHERE giveaway_id = @Id";
                const string updateQuery = "UPDATE #list SET closed = @Closed, ended = @Ended, winner = @Winner WHERE id = @Id";
                const string closedConst = "1";
                const string endedConst = "1";

                QueryParametersBuilder parameters = new QueryParametersBuilder();
                parameters.Add("@Id", id);
                parameters.Add("@Closed", closedConst);
                parameters.Add("@Ended", endedConst);

                var data = await Database.SelectQueryAsync(selectGiveawayDataQuery, parameters, Config);
                if (data.Count == 0) { await command.RespondAsync("Wrong id!", ephemeral: true); return; }
                string ended = data[1];

                if (ended != "0")
                {
                    await command.RespondAsync("Winners has been already rolled!", ephemeral: true);
                    return;
                }

                int maxWinners = int.Parse(data[0]);

                var giveawayUsers = await Database.SelectQueryAsync(selectGiveawayUsersQuery, parameters, Config);

                List<string> winners = new List<string>();

                Random rng = new Random();

                for (int i = 0; i < maxWinners; i++)
                {
                    int pick = rng.Next(0, giveawayUsers.Count);
                    while (true)
                    {
                        if (winners.Count == giveawayUsers.Count) break;

                        if (winners.Contains(giveawayUsers[pick]))
                        {
                            pick = rng.Next(0, giveawayUsers.Count);
                        }
                        else
                            break;
                    }
                    if (winners.Count < giveawayUsers.Count)
                        winners.Add(giveawayUsers[pick]);
                }

                string jsonWinners = JsonSerializer.Serialize(winners);

                parameters.Add("@winner", jsonWinners);

                int result = await Database.UpdateQueryAsync(updateQuery, parameters, Config);
                string str = "The winners are: ";
                foreach (var w in winners)
                {
                    str += "<@" + w + ">, ";
                }
                await command.RespondAsync(str);

                var channel = command.Channel as ITextChannel;
                var msg = await channel.GetMessageAsync(ulong.Parse(data[2]));
                var embed = msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                var newEmbed = await EditGiveawayEmbed(embed, options: new EmbedOptions[] { EmbedOptions.CloseGiveaway });
                if (data[3] == "0")
                    await channel.ModifyMessageAsync(msg.Id, x => { x.Embed = newEmbed.Build(); });
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public async Task List(SocketSlashCommand command, Dictionary<string, string> args)
        {
            //const string selectQuery = "SELECT ";
            throw new NotImplementedException();
        }
        public async Task<GiveawayStatus> GiveawayClosed(string giveawayId)
        {
            const string selectQuery = "Select closed,ended FROM #list WHERE id = @GiveawayId";

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@GiveawayId", giveawayId);

            var data = await Database.SelectQueryAsync(selectQuery, parameters, Config);

            if (data[0] == "0" && data[1] == "0") return GiveawayStatus.Open;
            return GiveawayStatus.Closed;
        }
        public async Task<GiveawayStatus> AddUserAsync(string giveawayId, string userId)
        {
            var status = await GiveawayClosed(giveawayId);
            if (status == GiveawayStatus.Closed) return GiveawayStatus.Closed;

            const string selectQuery = "SELECT * FROM #users WHERE user_id = @UserId AND giveaway_id = @GiveawayId";
            const string insertQuery = "INSERT INTO #users (giveaway_id, user_id) VALUES (@GiveawayId, @UserId)";

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@GiveawayId", giveawayId);
            parameters.Add("@UserId", userId);

            var list = await Database.SelectQueryAsync(selectQuery, parameters, Config);
            if (list.Any())
            {
                return GiveawayStatus.UserExist;
            }
            else
            {
                var result = await Database.InsertQueryAsync(insertQuery, parameters, Config);
                if (result > 0)
                    return GiveawayStatus.UserAdded;
                return GiveawayStatus.UserAddFail;
            }
        }
        public async Task<GiveawayStatus> RemoveUserAsync(string giveawayId, string userId)
        {
            var status = await GiveawayClosed(giveawayId);
            if (status == GiveawayStatus.Closed) return GiveawayStatus.Closed;

            const string selectQuery = "DELETE FROM #users WHERE giveaway_id = @GiveawayId AND user_id = @UserId";

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@GiveawayId", giveawayId);
            parameters.Add("@UserId", userId);

            var result = await Database.DeleteQueryAsync(selectQuery, parameters, Config);
            if (result > 0)
                return GiveawayStatus.UserRemoved;
            return GiveawayStatus.UserRemoveFail;
        }
        public async Task UpdateList()
        {
            const string selectQuery = "SELECT id FROM #list WHERE ended = @Ended";
            const string ended = "0";

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@Ended",ended);

            var list = await Database.SelectQueryAsync(selectQuery, parameters, Config);
            if (list.Count > 0)
            {
                activeGiveaways = list;
            }
        }

        public static List<string> GetActiveGiveaways()
        {
            return activeGiveaways;
        }

        public async Task<Task> HandleComponent(SocketMessageComponent component)
        {
            Console.WriteLine("Component executed in giveaway");
            await UpdateList();
            try
            {
                var activeGiveaways = GetActiveGiveaways();
                foreach (var data in activeGiveaways)
                { 
                    if (data == component.Data.CustomId)
                    {
                        var result = await AddUserAsync(data.ToString(), component.User.Id.ToString());
                        if (result == GiveawayStatus.UserAdded)
                        {
                            var embed = component.Message.Embeds.First().ToEmbedBuilder();
                            var newEmbed = await EditGiveawayEmbed(embed, component.Data.CustomId, options: new EmbedOptions[] { EmbedOptions.increase });

                            await component.Message.ModifyAsync(x =>
                            {
                                x.Embed = newEmbed.Build();
                            });

                            await component.RespondAsync("You successfully joined giveaway!", ephemeral: true);
                        }
                        else if (result == GiveawayStatus.UserExist)
                        {
                            var button = new ComponentBuilder().WithButton("Leave", component.Data.CustomId + "_remove", ButtonStyle.Danger);
                            await component.RespondAsync("You already joined this giveaway!", components: button.Build(), ephemeral: true);
                        }
                        else if (result == GiveawayStatus.Closed)
                        {
                            await component.RespondAsync("You can't join giveaway that is closed!", ephemeral: true);
                        }
                        else if (result == GiveawayStatus.UserAddFail)
                        {
                            await component.RespondAsync("We can not add you to this giveaway, contact server owner!", ephemeral: true);
                        }
                        break;
                    }
                    else if (component.Data.CustomId.ToString().Contains("_remove") && component.Data.CustomId.Contains(data))
                    {
                        string customId = component.Data.CustomId[..component.Data.CustomId.IndexOf("_remove")];
                        var result = await RemoveUserAsync(customId, component.User.Id.ToString());

                        const string selectQuery = "SELECT message_id FROM #list where id = @CustomId";

                        QueryParametersBuilder parameters = new QueryParametersBuilder();
                        parameters.Add("@CustomId", customId);

                        var messageId = await Database.SelectQueryAsync(selectQuery, parameters, Config);

                        IMessage message = await component.Channel.GetMessageAsync(ulong.Parse(messageId.First()));

                        if (message == null)
                        {
                            await component.RespondAsync("This giveaway does not exists!", ephemeral: true);
                            break;
                        }

                        if (result == GiveawayStatus.UserRemoved)
                        {
                            var embed = message.Embeds.First().ToEmbedBuilder(); //get old embed to edit
                            var newEmbed = await EditGiveawayEmbed(embed, customId, options: new EmbedOptions[] { EmbedOptions.decrease });

                            await component.Channel.ModifyMessageAsync(ulong.Parse(messageId.First()), m =>
                            {
                                m.Embed = newEmbed.Build();
                            });

                            await component.RespondAsync("You left the giveaway!", ephemeral: true);
                        }
                        else if (result == GiveawayStatus.UserRemoveFail)
                        {
                            await component.RespondAsync("You did not sign up for this giveaway!", ephemeral: true);
                        }
                        else if (result == GiveawayStatus.Closed)
                        {
                            await component.RespondAsync("You can't leave giveaway that is closed!", ephemeral: true);
                        }
                        break;
                    }
                }
            }catch(Exception ex)
            {
                Console.WriteLine("Giveaway plugin error in component handler: " + ex.Message);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task<Func<Task>> Update(SocketGuild guild)
        {
            DateTime date = DateTime.Now;
            string dateNow = date.ToString("yyyy-MM-dd HH:mm:ss");
            const string selectQuery = "SELECT guild_id,channel_id,message_id,closed FROM #list WHERE ends <= @Ends AND closed = 0";

            QueryParametersBuilder parameters = new QueryParametersBuilder();
            parameters.Add("@Ends", dateNow);

            var result = await Database.SelectQueryAsync(selectQuery, parameters, Config);
            const int setSize = 4;
            while (result.Count > 0)
            {
                var currentSet = result.Take(setSize).ToList();
                result = result.Skip(setSize).ToList();

                ulong guildId = ulong.Parse(currentSet[0]);
                ulong channelId = ulong.Parse(currentSet[1]);
                ulong messageId = ulong.Parse(currentSet[2]);
                string closed = currentSet[3];

                if (closed == "0")
                {
                    var channel = guild.GetTextChannel(channelId);
                    var messageData = await channel.GetMessageAsync(messageId);

                    var embed = messageData.Embeds.First().ToEmbedBuilder();
                    var newEmbed = await EditGiveawayEmbed(embed, options: new EmbedOptions[] { EmbedOptions.CloseGiveaway });

                    if (newEmbed == null) newEmbed = embed;
                    
                    Func<Task> func = async () =>
                    {
                        const string updateQuery = "UPDATE #list SET closed = @Closed WHERE message_id = @MessageId";

                        parameters.Add("@MessageId", messageId.ToString());
                        parameters.Add("@Closed", "1");
                        await channel.ModifyMessageAsync(messageId, x => x.Embed = newEmbed.Build());
                        await Database.UpdateQueryAsync(updateQuery, parameters, Config);
                        Logger.Log(Config.pluginName, "Closed giveaway!", LogLevel.Info);
                    };
                    return func;
                }
            }
            return null;
        }

        public Task<Task> ClientReady()
        {
            throw new NotImplementedException();
        }

        public Task<object> UserCommandExecuted(SocketUserCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<object> MessageCommandExecuted(SocketMessageCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
