﻿using Discord;
using Discord.Webhook;
using script_chan2.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace script_chan2.Discord
{
    public static class DiscordApi
    {
        public static void SendMatchCreated(Match match)
        {
            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = "https://cdn.discordapp.com/attachments/130304896581763072/400731693192839179/plus.png",
                    Name = match.Name,
                    Url = "https://osu.ppy.sh/community/matches/" + match.RoomId
                },
                Color = Color.Blue,
                Title = "The match has been created!",
                Description = $"You can join the match on IRC by typing ```/join #mp_{match.RoomId}```"
            };

            SendEmbed(match, embed);
        }

        public static void SendMatchBanRecap(Match match)
        {
            EmbedBuilder embed = null;
            if (match.TeamMode == Enums.TeamModes.TeamVS)
            {
                embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = "https://cdn.discordapp.com/attachments/130304896581763072/400744720772628481/more-info-button.png",
                        Name = match.Name,
                        Url = "https://osu.ppy.sh/community/matches/" + match.RoomId
                    },
                    Color = Color.Gold,
                    Url = "https://osu.ppy.sh/community/matches/" + match.RoomId,
                    Title = "Ban Recap " + (match.RollWinnerTeam != null ? "(Roll Winner: " + match.RollWinnerTeam.Name + ")" : ""),
                };
                var redTeam = "";
                var blueTeam = "";
                foreach (var ban in match.Bans)
                {
                    var mod = ban.Map.Tag;
                    if (string.IsNullOrEmpty(mod))
                        mod = Utils.ConvertGameModsToString(ban.Map.Mods);
                    if (ban.Team == match.TeamRed)
                        redTeam += $"__{mod}__ **{ban.Map.Beatmap.Artist.Replace("_", "__").Replace("*", "\\*")} - {ban.Map.Beatmap.Title.Replace("_", "__").Replace("*", "\\*")} [{ban.Map.Beatmap.Version.Replace("_", "__").Replace("*", "\\*")}]**" + Environment.NewLine;
                    if (ban.Team == match.TeamBlue)
                        blueTeam += $"__{mod}__ **{ban.Map.Beatmap.Artist.Replace("_", "__").Replace("*", "\\*")} - {ban.Map.Beatmap.Title.Replace("_", "__").Replace("*", "\\*")} [{ban.Map.Beatmap.Version.Replace("_", "__").Replace("*", "\\*")}]**" + Environment.NewLine;
                }
                if (!string.IsNullOrEmpty(redTeam))
                    embed.Fields.Add(new EmbedFieldBuilder { Name = match.TeamRed.Name, Value = redTeam });
                if (!string.IsNullOrEmpty(blueTeam))
                    embed.Fields.Add(new EmbedFieldBuilder { Name = match.TeamBlue.Name, Value = blueTeam });
            }
            else
            {
                //TODO!
            }

            if (embed != null)
                SendEmbed(match, embed);
        }

        public static void SendMatchPickRecap(Match match)
        {
            EmbedBuilder embed = null;
            if (match.TeamMode == Enums.TeamModes.TeamVS)
            {
                embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = "https://cdn.discordapp.com/attachments/130304896581763072/400744720772628481/more-info-button.png",
                        Name = match.Name,
                        Url = "https://osu.ppy.sh/community/matches/" + match.RoomId
                    },
                    Color = Color.Gold,
                    Url = "https://osu.ppy.sh/community/matches/" + match.RoomId,
                    Title = "Pick Recap " + (match.RollWinnerTeam != null ? "(Roll Winner: " + match.RollWinnerTeam.Name + ")" : ""),
                };
                var redTeam = "";
                var blueTeam = "";
                for (var i = 0; i < match.Picks.Count; i++)
                {
                    var pick = match.Picks[i];
                    var mod = pick.Map.Tag;
                    if (string.IsNullOrEmpty(mod))
                        mod = Utils.ConvertGameModsToString(pick.Map.Mods);
                    if (pick.Team == match.TeamRed)
                        redTeam += $"-{i + 1}- __{mod}__ **{pick.Map.Beatmap.Artist.Replace("_", "__").Replace("*", "\\*")} - {pick.Map.Beatmap.Title.Replace("_", "__").Replace("*", "\\*")} [{pick.Map.Beatmap.Version.Replace("_", "__").Replace("*", "\\*")}]**" + Environment.NewLine;
                    if (pick.Team == match.TeamBlue)
                        blueTeam += $"-{i + 1}- __{mod}__ **{pick.Map.Beatmap.Artist.Replace("_", "__").Replace("*", "\\*")} - {pick.Map.Beatmap.Title.Replace("_", "__").Replace("*", "\\*")} [{pick.Map.Beatmap.Version.Replace("_", "__").Replace("*", "\\*")}]**" + Environment.NewLine;
                }
                if (!string.IsNullOrEmpty(redTeam))
                    embed.Fields.Add(new EmbedFieldBuilder { Name = match.TeamRed.Name, Value = redTeam });
                if (!string.IsNullOrEmpty(blueTeam))
                    embed.Fields.Add(new EmbedFieldBuilder { Name = match.TeamBlue.Name, Value = blueTeam });
            }
            else
            {
                //TODO!
            }

            if (embed != null)
                SendEmbed(match, embed);
        }

        public static void SendGameRecap(Match match)
        {
            EmbedBuilder embed = null;
            if (match.TeamMode == Enums.TeamModes.TeamVS)
            {
                embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = "https://cdn0.iconfinder.com/data/icons/fighting-1/258/brawl003-512.png",
                        Name = match.Name,
                        Url = "https://osu.ppy.sh/community/matches/" + match.RoomId
                    }
                };

                bool pickingTeamWon = false;
                var teamRedScore = 0;
                var teamBlueScore = 0;
                foreach (var score in match.Games.Last().Scores)
                {
                    if (score.Passed)
                    {
                        if (match.TeamRed.Players.Any(x => x.Id == score.Player.Id))
                            teamRedScore += score.Points;
                        if (match.TeamBlue.Players.Any(x => x.Id == score.Player.Id))
                            teamBlueScore += score.Points;
                    }
                }
                if (teamRedScore > teamBlueScore && match.Picks.Last().Team == match.TeamRed)
                    pickingTeamWon = true;
                if (teamBlueScore > teamRedScore && match.Picks.Last().Team == match.TeamBlue)
                    pickingTeamWon = true;

                var map = match.Picks.Last();

                var mod = map.Map.Tag;
                if (string.IsNullOrEmpty(mod))
                    mod = Utils.ConvertGameModsToString(map.Map.Mods);

                var mvp = match.Games.Last().Scores.OrderByDescending(x => x.Points).First();

                embed.Title = $"{map.Team.Name} {(pickingTeamWon ? "won" : "lost")} their __{mod}__ pick by {string.Format("{0:n0}", Math.Abs(teamRedScore - teamBlueScore))}";
                embed.Color = pickingTeamWon ? Color.Green : Color.Red;
                embed.ThumbnailUrl = pickingTeamWon ? "https://cdn.discordapp.com/attachments/130304896581763072/400388818127290369/section-pass.png" : "https://cdn.discordapp.com/attachments/130304896581763072/400388814213873666/section-fail.png";
                embed.Description = $"**{map.Map.Beatmap.Artist.Replace("_", "__").Replace("*", "\\*")} - {map.Map.Beatmap.Title.Replace("_", "__").Replace("*", "\\*")} [{map.Map.Beatmap.Version.Replace("_", "__").Replace("*", "\\*")}]**";
                embed.Fields.Add(new EmbedFieldBuilder { Name = match.TeamRed.Name, Value = teamRedScore, IsInline = true });
                embed.Fields.Add(new EmbedFieldBuilder { Name = match.TeamBlue.Name, Value = teamBlueScore, IsInline = true });
                embed.Fields.Add(new EmbedFieldBuilder { Name = "MVP", Value = $":flag_{mvp.Player.Country.ToLower()}: **{mvp.Player.Name.Replace("_", "__")}** with {string.Format("{0:n0}", mvp.Points)} points" });
                embed.Fields.Add(new EmbedFieldBuilder { Name = "Status", Value = "Next team to pick: " + (map.Team == match.TeamRed ? match.TeamBlue.Name : match.TeamRed.Name) + " :loudspeaker:" });
            }
            else
            {
                //TODO!
            }

            if (embed != null)
                SendEmbed(match, embed);
        }

        private static void SendEmbed(Match match, EmbedBuilder embed)
        {
            embed.Footer = new EmbedFooterBuilder
            {
                Text = "Woah! So cool! :smirk:",
                IconUrl = "https://i.imgur.com/fKL31aD.jpg"
            };
            foreach (var webhook in match.Tournament.Webhooks)
            {
                using (var client = new DiscordWebhookClient(webhook.URL))
                {
                    client.SendMessageAsync(embeds: new[] { embed.Build() }, username: "Script-chan", avatarUrl: "https://cdn.discordapp.com/attachments/130304896581763072/400723356283961354/d366ce5fdd90f4e4471da04db380c378.png").GetAwaiter().GetResult();
                }
            }
        }
    }
}
