using log4net;
using NosGm.Parser.Import;
using NosGm.Core;
using NosGm.DAL.EF.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NosGm.Parser
{
    public class Program
    {
        #region Members

        private static ImportConfiguration configuration;

        #endregion

        #region Methods

        private static void Init()
        {
            Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));
            configuration = new ImportConfiguration
            {
                Folder = string.Empty,
                Lang = "uk",
                Packets = new List<string[]>(),
                LangFolder = string.Empty,
                DatFolder = string.Empty,
                PacketFolder = string.Empty
            };
        }

        private static void Main(string[] args)
        {
            Init();
            PrintHeader();
            RequiredFiles();
            DataAccessHelper.Initialize();

            try
            {
                Logger.Warn(Language.Instance.GetMessageFromKey("ENTER_PATH"));
                configuration.Folder = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(configuration.Folder))
                {
                    configuration.Folder = Directory.GetCurrentDirectory() + "/parser";
                }

                var folder = configuration.Folder;
                configuration.LangFolder = folder + $"\\Lang\\{configuration.Lang}_{configuration.Lang}";
                configuration.DatFolder = folder + "\\Dat\\";
                configuration.PacketFolder = folder + "\\Packet\\";
                configuration.MapFolder = folder + "\\Map\\";

                new ImportPackets(configuration).Import();

                if (AskToParse($"{Language.Instance.GetMessageFromKey("PARSE_ALL")} [Y/n]").KeyChar != 'n')
                {
                    new ImportMaps(configuration).Import();
                    new ImportSecondaryMaps(configuration).Import();
                    new ImportRespawnMapType().Import();
                    new ImportMapType().Import();
                    new ImportMapTypeMap().Import();
                    new ImportAccounts().Import();
                    new ImportPortals(configuration).Import();
                    new ImportScriptedInstances(configuration).Import();
                    new ImportItems(configuration).Import();
                    new ImportSkills(configuration).Import();
                    new ImportCards(configuration).Import();
                    new ImportNpcMonsters(configuration).Import();
                    new ImportNpcMonsterData(configuration).Import();
                    new ImportDrops().Import();
                    new ImportMapNpcs(configuration).Import();
                    new ImportMonsters(configuration).Import();
                    new ImportShops(configuration).Import();
                    new ImportTeleporters(configuration).Import();
                    new ImportShopItems(configuration).Import();
                    new ImportShopSkills(configuration).Import();
                    new ImportRecipe(configuration).Import();
                    new ImportHardcodedRecipes().Import();
                    new ImportQuests(configuration).Import();
                    new ImportFishPosition(configuration).Import();
                }
                else
                {
                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_MAPS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportMaps(configuration).Import();
                        new ImportSecondaryMaps(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_MAPTYPES")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportMapType().Import();
                        new ImportMapTypeMap().Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_ACCOUNTS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportAccounts().Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_PORTALS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportPortals(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_TIMESPACES")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportScriptedInstances(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_ITEMS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportItems(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_SKILLS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportSkills(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_MONSTERS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportNpcMonsters(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_NPCMONSTERDATA")} [Y/n]").KeyChar !=
                        'n')
                    {
                        new ImportNpcMonsterData(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_DROPS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportDrops().Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_CARDS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportCards(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_MAPNPCS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportMapNpcs(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_MAPMONSTERS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportMonsters(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_SHOPS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportShops(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_TELEPORTERS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportTeleporters(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_SHOPITEMS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportShopItems(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_SHOPSKILLS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportShopSkills(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_RECIPES")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportRecipe(configuration).Import();
                        new ImportHardcodedRecipes().Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_QUESTS")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportQuests(configuration).Import();
                    }

                    if (AskToParse($@"{Language.Instance.GetMessageFromKey("PARSE_FISH")} [Y/n]").KeyChar != 'n')
                    {
                        new ImportFishPosition(configuration).Import();
                    }
                }

                Console.WriteLine(Language.Instance.GetMessageFromKey("DONE"));
                Console.ReadKey();
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error(Language.Instance.GetMessageFromKey("AT_LEAST_ONE_FILE_MISSING"), ex);
                Console.ReadKey();
            }
        }

        private static ConsoleKeyInfo AskToParse(string msg)
        {
            Console.WriteLine(msg);
            return Console.ReadKey(true);
        }

        private static void PrintHeader()
        {
            Console.Title = "NosGm - Parser";
            const string text = @"

 ______ _____   ____   _____ _________      ________ _____ _   _ 
|  ____|  __ \ / __ \ / ____|__   __\ \    / /  ____|_   _| \ | |
| |__  | |__) | |  | | (___    | |   \ \  / /| |__    | | |  \| |
|  __| |  _  /| |  | |\___ \   | |    \ \/ / |  __|   | | | . ` |
| |    | | \ \| |__| |____) |  | |     \  /  | |____ _| |_| |\  |
|_|    |_|  \_\\____/|_____/   |_|      \/   |______|_____|_| \_|
                                                                                           
";
            string separator = new string('=', Console.WindowWidth);
            string logo = text.Split('\n').Select(s => string.Format("{0," + (Console.WindowWidth / 2 + s.Length / 2) + "}\n", s)).Aggregate("", (current, i) => current + i);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(separator + logo + separator);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void RequiredFiles()
        {
            
        }

        #endregion
    }
}