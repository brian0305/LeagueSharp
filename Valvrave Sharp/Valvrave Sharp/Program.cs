namespace Valvrave_Sharp
{
    using System;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.UI.IMenu.Customizer;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.UI.INotifications;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

    using SharpDX;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal class Program
    {
        #region Static Fields

        public static Items.Item Bilgewater, BotRuinedKing, Youmuu, Hydra, Tiamat;

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
                500,
                () =>
                    {
                        MenuCustomizer.Instance.Parent["orbwalker"]["lasthitKey"].DisplayName = "Last Hit";
                        Player = GameObjects.Player;
                        MainMenu = new Menu("ValvraveSharp", "Valvrave Sharp", true, Player.ChampionName).Attach();
                        AddUI.Separator(MainMenu, "Author", "Author: Brian");
                        AddUI.Separator(MainMenu, "Paypal", "Paypal: dcbrian01@gmail.com");
                        NewInstance(plugin);
                        Bilgewater = new Items.Item(ItemId.Bilgewater_Cutlass, 550);
                        BotRuinedKing = new Items.Item(ItemId.Blade_of_the_Ruined_King, 550);
                        Youmuu = new Items.Item(ItemId.Youmuus_Ghostblade, 0);
                        Tiamat = new Items.Item(ItemId.Tiamat_Melee_Only, 400);
                        Hydra = new Items.Item(ItemId.Ravenous_Hydra_Melee_Only, 400);
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

    internal class AddUI : Program
    {
        #region Public Methods and Operators

        public static MenuBool Bool(Menu subMenu, string name, string display, bool state = true)
        {
            return subMenu.Add(new MenuBool(name, display, state));
        }

        public static MenuKeyBind KeyBind(
            Menu subMenu,
            string name,
            string display,
            Keys key,
            KeyBindType type = KeyBindType.Press)
        {
            return subMenu.Add(new MenuKeyBind(name, display, key, type));
        }

        public static MenuList List(Menu subMenu, string name, string display, string[] array)
        {
            return subMenu.Add(new MenuList<string>(name, display, array));
        }

        public static void Notif(string msg)
        {
            Notifications.Add(new Notification("Valvrave", msg));
        }

        public static MenuSeparator Separator(Menu subMenu, string name, string display)
        {
            return subMenu.Add(new MenuSeparator(name, display));
        }

        public static MenuSlider Slider(Menu subMenu, string name, string display, int cur, int min = 0, int max = 100)
        {
            return subMenu.Add(new MenuSlider(name, display, cur, min, max));
        }

        #endregion
    }

    internal class Common
    {
        #region Public Methods and Operators

        public static bool CanUseSkill(OrbwalkerMode mode)
        {
            return Orbwalker.CanMove && (!Orbwalker.CanAttack || Orbwalker.GetTarget(mode) == null);
        }

        public static int CountEnemy(float range, Vector3 pos = default(Vector3))
        {
            return GameObjects.EnemyHeroes.Count(i => i.IsValidTarget(range, true, pos));
        }

        #endregion
    }
}