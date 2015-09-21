namespace Valvrave_Sharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.UI.IMenu;
    using LeagueSharp.SDK.Core.Wrappers;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Plugin;

    internal class Program
    {
        #region Static Fields

        public static Menu MainMenu;

        internal static Items.Item Bilgewater, BotRuinedKing, Youmuu, Tiamat, Hydra, Titanic;

        internal static SpellSlot Flash, Ignite, Smite;

        internal static Spell Q, Q2, W, W2, E, E2, R, R2;

        private static readonly Dictionary<string, Func<object>> Plugins = new Dictionary<string, Func<object>>
                                                                               {
                                                                                   { "Yasuo", () => new Yasuo() },
                                                                                   { "Zed", () => new Zed() }
                                                                               };

        #endregion

        #region Properties

        internal static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        #endregion

        #region Methods

        private static void InitItem()
        {
            Bilgewater = new Items.Item(ItemId.Bilgewater_Cutlass, 550);
            BotRuinedKing = new Items.Item(ItemId.Blade_of_the_Ruined_King, 550);
            Youmuu = new Items.Item(ItemId.Youmuus_Ghostblade, 0);
            Tiamat = new Items.Item(ItemId.Tiamat_Melee_Only, 400);
            Hydra = new Items.Item(ItemId.Ravenous_Hydra_Melee_Only, 400);
            Titanic = new Items.Item(3053, 400);
        }

        private static void InitSummonerSpell()
        {
            foreach (var spell in
                Player.Spellbook.Spells.Where(
                    i =>
                    i.Name.ToLower().Contains("smite")
                    && (i.Slot == SpellSlot.Summoner1 || i.Slot == SpellSlot.Summoner2)))
            {
                Smite = spell.Slot;
            }
            Ignite = Player.GetSpellSlot("summonerdot");
            Flash = Player.GetSpellSlot("summonerflash");
        }

        private static void Main(string[] args)
        {
            if (args == null)
            {
                return;
            }
            Load.OnLoad += (sender, eventArgs) =>
                {
                    UpdateCheck();
                    if (!Plugins.ContainsKey(Player.ChampionName))
                    {
                        Game.PrintChat(string.Format("Valvrave Sharp => {0} Not Support!", Player.ChampionName));
                        return;
                    }
                    InitItem();
                    InitSummonerSpell();
                    MainMenu = new Menu("ValvraveSharp", "Valvrave Sharp", true, Player.ChampionName).Attach();
                    MainMenu.Separator("Author: Brian");
                    MainMenu.Separator("Paypal: dcbrian01@gmail.com");
                    Plugins[Player.ChampionName].Invoke();
                };
        }

        private static void UpdateCheck()
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
                            string.Format(
                                "{0}.{1}.{2}.{3}",
                                checkFile.Groups[1],
                                checkFile.Groups[2],
                                checkFile.Groups[3],
                                checkFile.Groups[4]));
                    if (gitVersion > Assembly.GetExecutingAssembly().GetName().Version)
                    {
                        Game.PrintChat(string.Format("Valvrave Sharp => Outdated! Newest Version: {0}", gitVersion));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion
    }
}