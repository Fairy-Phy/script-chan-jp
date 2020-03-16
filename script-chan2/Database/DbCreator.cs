﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace script_chan2.Database
{
    public static class DbCreator
    {
        private static SQLiteConnection conn;

        public static void CreateDb()
        {
            Log.Information("DB creation started");
            SQLiteConnection.CreateFile("Database.sqlite");
            conn = new SQLiteConnection("Data Source=Database.sqlite;Version=3");
            conn.Open();
            CreateUserSettingsTable();
            CreateTournamentsTable();
            CreateWebhooksTable();
            CreateWebhookLinksTable();
            CreateMappoolsTable();
            CreateBeatmapsTable();
            CreateMappoolMapsTable();
            CreateTeamsTable();
            CreatePlayersTable();
            CreateTeamPlayersTable();
            CreateMatchesTable();
            CreateMatchPlayersTable();
            CreateMatchPicksTable();
            CreateGamesTable();
            CreateScoresTable();
            conn.Close();
            Log.Information("DB creation finished");
        }

        public static bool DbExists
        {
            get { return File.Exists("Database.sqlite"); }
        }

        private static void CreateUserSettingsTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE UserSettings
                (name TEXT NOT NULL PRIMARY KEY,
                value TEXT)", conn);
            command.ExecuteNonQuery();
            command.Dispose();
            command = new SQLiteCommand(@"INSERT INTO UserSettings (name, value) VALUES
                ('lang', 'en-US'),
                ('ircTimeout', '1000'),
                ('enablePrivateIrc', 'False'),
                ('defaultBO', '3'),
                ('defaultTournament', ''),
                ('defaultTimerCommand', '120'),
                ('defaultTimerAfterGame', '120'),
                ('defaultTimerAfterPick', '120')", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateTournamentsTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Tournaments
                (id INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                gameMode TEXT,
                teamMode TEXT,
                winCondition TEXT,
                acronym TEXT,
                teamSize INTEGER,
                roomSize INTEGER,
                pointsForSecondBan INTEGER,
                allPicksFreemod BOOL,
                mpTimerCommand INTEGER,
                mpTimerAfterGame INTEGER,
                mpTimerAfterPick INTEGER,
                welcomeString TEXT)", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateWebhooksTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Webhooks
                (id INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                url TEXT)", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateWebhookLinksTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE WebhookLinks
                (tournament INTEGER NOT NULL,
                webhook INTEGER NOT NULL,
                PRIMARY KEY(tournament, webhook),
                FOREIGN KEY(tournament) REFERENCES Tournaments(id),
                FOREIGN KEY(webhook) REFERENCES Webhooks(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateMappoolsTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Mappools
                (id INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                tournament INTEGER,
                FOREIGN KEY(tournament) REFERENCES Tournaments(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateBeatmapsTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Beatmaps
                (id INTEGER NOT NULL PRIMARY KEY,
                beatmapsetId INT,
                artist TEXT,
                title TEXT,
                version TEXT,
                creator TEXT)", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateMappoolMapsTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE MappoolMaps
                (id INTEGER NOT NULL PRIMARY KEY,
                mappool INTEGER NOT NULL,
                beatmap INTEGER NOT NULL,
                listIndex INTEGER,
                mods TEXT,
                FOREIGN KEY(mappool) REFERENCES Mappools(id),
                FOREIGN KEY(beatmap) REFERENCES Beatmaps(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateTeamsTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Teams
                (id INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                tournament INTEGER,
                FOREIGN KEY(tournament) REFERENCES Tournaments(id),
                CONSTRAINT unique_team UNIQUE (name, tournament))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreatePlayersTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Players
                (id INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                country TEXT)", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateTeamPlayersTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE TeamPlayers
                (player INTEGER NOT NULL,
                team INTEGER NOT NULL,
                PRIMARY KEY (player, team),
                FOREIGN KEY(player) REFERENCES Players(id),
                FOREIGN KEY(team) REFERENCES Teams(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateMatchesTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Matches
                (id INTEGER NOT NULL PRIMARY KEY,
                name TEXT,
                roomId INTEGER,
                tournament INTEGER,
                mappool TEXT,
                gameMode TEXT,
                teamMode TEXT,
                winCondition TEXT,
                teamBlue INTEGER,
                teamBluePoints INTEGER,
                teamRed INTEGER,
                teamRedPoints INTEGER,
                teamSize INTEGER,
                roomSize INTEGER,
                rollWinner INTEGER,
                firstPicker INTEGER,
                BO INTEGER,
                enableWebhooks BOOL,
                mpTimerCommand INTEGER,
                mpTimerAfterGame INTEGER,
                mpTimerAfterPick INTEGER,
                pointsForSecondBan INTEGER,
                allPicksFreemod BOOL,
                status TEXT,
                FOREIGN KEY(tournament) REFERENCES Tournaments(id),
                FOREIGN KEY(mappool) REFERENCES Mappools(id),
                FOREIGN KEY(teamBlue) REFERENCES Teams(id),
                FOREIGN KEY(teamRed) REFERENCES Teams(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateMatchPlayersTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE MatchPlayers
                (match INTEGER NOT NULL,
                player INTEGER NOT NULL,
                PRIMARY KEY(match, player),
                FOREIGN KEY(match) REFERENCES Matches(id),
                FOREIGN KEY(player) REFERENCES Players(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateMatchPicksTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE MatchPicks
                (match INTEGER NOT NULL,
                beatmap INTEGER NOT NULL,
                picker INTEGER NOT NULL,
                PRIMARY KEY(match, beatmap),
                FOREIGN KEY(match) REFERENCES Matches(id),
                FOREIGN KEY(beatmap) REFERENCES MappoolMaps(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateGamesTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Games
                (id INTEGER NOT NULL PRIMARY KEY,
                match INTEGER,
                beatmap INTEGER,
                mods TEXT,
                counts BOOL,
                FOREIGN KEY(match) REFERENCES Matches(id),
                FOREIGN KEY(beatmap) REFERENCES Beatmaps(id))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        private static void CreateScoresTable()
        {
            var command = new SQLiteCommand(@"CREATE TABLE Scores
                (player INTEGER NOT NULL,
                game INTEGER NOT NULL,
                mods TEXT,
                score INTEGER,
                team TEXT,
                passed BOOL,
                FOREIGN KEY(player) REFERENCES Players(id),
                FOREIGN KEY(game) REFERENCES Games(id),
                CONSTRAINT unique_score UNIQUE (player, game))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
        }
    }
}
