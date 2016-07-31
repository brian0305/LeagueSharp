namespace Valvrave_Sharp
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Plugin;

    #endregion

    internal class Program
    {
        #region Constants

        internal const int FlashRange = 425, IgniteRange = 600, SmiteRange = 570;

        #endregion

        #region Static Fields

        internal static Items.Item Bilgewater, BotRuinedKing, Youmuu, Tiamat, Hydra, Titanic;

        internal static SpellSlot Flash = SpellSlot.Unknown, Ignite = SpellSlot.Unknown, Smite = SpellSlot.Unknown;

        internal static Menu MainMenu;

        internal static Obj_AI_Hero Player;

        internal static Spell Q, Q2, Q3, W, W2, E, E2, R, R2, R3;

        private static readonly Dictionary<string, Tuple<Func<object>, int>> Plugins =
            new Dictionary<string, Tuple<Func<object>, int>>
                {
                    { "DrMundo", new Tuple<Func<object>, int>(() => new DrMundo(), 9) },
                    { "Kennen", new Tuple<Func<object>, int>(() => new Kennen(), 6) },
                    { "LeeSin", new Tuple<Func<object>, int>(() => new LeeSin(), 10) },
                    { "Vladimir", new Tuple<Func<object>, int>(() => new Vladimir(), 7) },
                    { "Yasuo", new Tuple<Func<object>, int>(() => new Yasuo(), 3) },
                    { "Zed", new Tuple<Func<object>, int>(() => new Zed(), 3) }
                };

        private static bool isSkinReset = true;

        #endregion

        #region Methods

        private static void CheckVersion()
        {
            try
            {
                using (var web = new WebClient())
                {
                    var rawFile =
                        web.DownloadString(
                            "https://raw.githubusercontent.com/brian0305/LeagueSharp/master/Valvrave%20Sharp/Valvrave%20Sharp/Properties/AssemblyInfo.cs");
                    var checkFile =
                        new Regex(@"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]").Match
                            (rawFile);
                    if (!checkFile.Success)
                    {
                        return;
                    }
                    var gitVersion =
                        new Version(
                            $"{checkFile.Groups[1]}.{checkFile.Groups[2]}.{checkFile.Groups[3]}.{checkFile.Groups[4]}");
                    if (gitVersion > Assembly.GetExecutingAssembly().GetName().Version)
                    {
                        PrintChat("Outdated! Newest Version: " + gitVersion);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void InitItem()
        {
            Bilgewater = new Items.Item(ItemId.Bilgewater_Cutlass, 550);
            BotRuinedKing = new Items.Item(ItemId.Blade_of_the_Ruined_King, 550);
            Youmuu = new Items.Item(ItemId.Youmuus_Ghostblade, 0);
            Tiamat = new Items.Item(ItemId.Tiamat_Melee_Only, 400);
            Hydra = new Items.Item(ItemId.Ravenous_Hydra_Melee_Only, 400);
            Titanic = new Items.Item(3748, 0);
        }

        private static void InitMenu(bool isSupport)
        {
            MainMenu = new Menu("ValvraveSharp", "Valvrave Sharp", true, Player.ChampionName).Attach();
            MainMenu.Separator("Author: Brian");
            MainMenu.Separator("Paypal: dcbrian01@gmail.com");
            if (isSupport)
            {
                var skinMenu = MainMenu.Add(new Menu("Skin", "Skin Changer"));
                {
                    skinMenu.Slider("Index", "Skin", 0, 0, Plugins[Player.ChampionName].Item2).ValueChanged +=
                        (sender, args) => { isSkinReset = true; };
                    skinMenu.Bool("Own", "Keep Your Own Skin").ValueChanged += (sender, args) =>
                        {
                            var menuBool = sender as MenuBool;
                            if (menuBool != null)
                            {
                                isSkinReset = false;
                                Player.SetSkin(
                                    Player.ChampionName,
                                    menuBool.Value && Player.BaseSkinId > 0
                                        ? Player.BaseSkinId
                                        : MainMenu["Skin"]["Index"]);
                            }
                        };
                }
                Plugins[Player.ChampionName].Item1.Invoke();

                Game.OnUpdate += args =>
                    {
                        if (Player.IsDead)
                        {
                            if (!isSkinReset)
                            {
                                isSkinReset = true;
                            }
                        }
                        else if (isSkinReset)
                        {
                            isSkinReset = false;
                            if (Player.BaseSkinId == 0 || !MainMenu["Skin"]["Own"])
                            {
                                Player.SetSkin(Player.ChampionName, MainMenu["Skin"]["Index"]);
                            }
                        }
                    };
            }
            else
            {
                MainMenu.Separator(Player.ChampionName + " Not Support");
            }
        }

        private static void InitSummonerSpell()
        {
            foreach (var smite in
                Player.Spellbook.Spells.Where(
                    i =>
                    (i.Slot == SpellSlot.Summoner1 || i.Slot == SpellSlot.Summoner2)
                    && i.Name.ToLower().Contains("smite")))
            {
                Smite = smite.Slot;
                break;
            }
            Ignite = Player.GetSpellSlot("SummonerDot");
            Flash = Player.GetSpellSlot("SummonerFlash");
        }

        private static void Main(string[] args)
        {
            if (args == null)
            {
                return;
            }
            Bootstrap.Init(args);
            Events.OnLoad += (sender, eventArgs) =>
                {
                    Player = GameObjects.Player;
                    CheckVersion();
                    var isSupport = Plugins.ContainsKey(Player.ChampionName);
                    InitMenu(isSupport);
                    if (!isSupport)
                    {
                        return;
                    }
                    InitItem();
                    InitSummonerSpell();
                };
        }

        private static void PrintChat(string text)
        {
            Game.PrintChat("Valvrave Sharp => {0}", text);
        }

        #endregion
    }
}