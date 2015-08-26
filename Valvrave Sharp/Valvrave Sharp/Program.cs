namespace Valvrave_Sharp
{
    using System;
    using System.Linq;
    using System.Reflection.Emit;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.UI.IMenu;
    using LeagueSharp.SDK.Core.UI.IMenu.Customizer;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

    using Valvrave_Sharp.Core;

    internal class Program
    {
        #region Static Fields

        public static Items.Item Bilgewater, BotRuinedKing, Youmuu, Tiamat, Hydra, Titanic;

        public static SpellSlot Flash, Ignite, Smite;

        public static Menu MainMenu;

        public static Spell Q, Q2, W, W2, E, E2, R, R2;

        #endregion

        #region Public Properties

        public static Obj_AI_Hero Player { get; set; }

        #endregion

        #region Methods

        private static void Main(string[] args)
        {
            if (args == null)
            {
                return;
            }
            Load.OnLoad += OnLoad;
        }

        private static void NewInstance(Type type)
        {
            var target = type.GetConstructor(Type.EmptyTypes);
            if (target == null || target.DeclaringType == null)
            {
                return;
            }
            var dynamic = new DynamicMethod(string.Empty, type, new Type[0], target.DeclaringType);
            var il = dynamic.GetILGenerator();
            il.DeclareLocal(target.DeclaringType);
            il.Emit(OpCodes.Newobj, target);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            ((Func<object>)dynamic.CreateDelegate(typeof(Func<object>)))();
        }

        private static void OnLoad(object sender, EventArgs e)
        {
            Player = ObjectManager.Player;
            var plugin = Type.GetType("Valvrave_Sharp.Plugin." + Player.ChampionName);
            if (plugin == null)
            {
                Game.PrintChat(Player.ChampionName + ": Not Load !");
                return;
            }
            Bootstrap.Init(null);
            DelayAction.Add(
                1000,
                () =>
                    {
                        MenuCustomizer.Instance.Parent["orbwalker"]["lasthitKey"].DisplayName = "Last Hit";
                        MainMenu = new Menu("ValvraveSharp", "Valvrave Sharp", true, Player.ChampionName).Attach();
                        Config.Separator(MainMenu, "Author", "Author: Brian");
                        Config.Separator(MainMenu, "Paypal", "Paypal: dcbrian01@gmail.com");
                        NewInstance(plugin);
                        Bilgewater = new Items.Item(ItemId.Bilgewater_Cutlass, 550);
                        BotRuinedKing = new Items.Item(ItemId.Blade_of_the_Ruined_King, 550);
                        Youmuu = new Items.Item(ItemId.Youmuus_Ghostblade, 0);
                        Tiamat = new Items.Item(ItemId.Tiamat_Melee_Only, 400);
                        Hydra = new Items.Item(ItemId.Ravenous_Hydra_Melee_Only, 400);
                        Titanic = new Items.Item(3053, 400);
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
                        Game.PrintChat(Player.ChampionName + ": Loaded !");
                    });
        }

        #endregion
    }
}