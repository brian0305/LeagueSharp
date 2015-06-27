using System;
using System.Linq;
using System.Reflection.Emit;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using ItemData = LeagueSharp.Common.Data.ItemData;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp
{
    internal class Program
    {
        public static Spell Q, Q2, W, W2, E, E2, R;
        public static SpellSlot Flash, Smite, Ignite;
        public static Items.Item Tiamat, Hydra, Youmuu, Zhonya, Seraph, Sheen, Iceborn, Trinity;
        public static Menu MainMenu;

        public static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Main(string[] args)
        {
            if (args == null)
            {
                return;
            }
            if (Game.Mode == GameMode.Running)
            {
                OnStart(new EventArgs());
            }
            Game.OnStart += OnStart;
        }

        private static void OnStart(EventArgs args)
        {
            var plugin = Type.GetType("BrianSharp.Plugin." + Player.ChampionName);
            if (plugin == null)
            {
                Helper.AddNotif(string.Format("[Brian Sharp] - {0}: Not Load !", Player.ChampionName), 3000);
                return;
            }
            MainMenu = new Menu("Brian Sharp", "BrianSharp", true);
            var infoMenu = new Menu("Info", "Info");
            {
                infoMenu.AddItem(new MenuItem("Author", "Author: Brian"));
                infoMenu.AddItem(new MenuItem("Paypal", "Paypal: dcbrian01@gmail.com"));
                MainMenu.AddSubMenu(infoMenu);
            }
            TargetSelector.AddToMenu(MainMenu.AddSubMenu(new Menu("Target Selector", "TS")));
            Orbwalk.AddToMainMenu(MainMenu);
            NewInstance(plugin);
            Helper.AddBool(
                MainMenu.SubMenu(Player.ChampionName + "_Plugin").SubMenu("Misc"), "UsePacket", "Use Packet To Cast");
            Tiamat = LeagueSharp.Common.Data.ItemData.Tiamat_Melee_Only.GetItem();
            Hydra = LeagueSharp.Common.Data.ItemData.Ravenous_Hydra_Melee_Only.GetItem();
            Youmuu = LeagueSharp.Common.Data.ItemData.Youmuus_Ghostblade.GetItem();
            Zhonya = LeagueSharp.Common.Data.ItemData.Zhonyas_Hourglass.GetItem();
            Seraph = LeagueSharp.Common.Data.ItemData.Seraphs_Embrace.GetItem();
            Sheen = LeagueSharp.Common.Data.ItemData.Sheen.GetItem();
            Iceborn = LeagueSharp.Common.Data.ItemData.Iceborn_Gauntlet.GetItem();
            Trinity = LeagueSharp.Common.Data.ItemData.Trinity_Force.GetItem();
            Flash = Player.GetSpellSlot("summonerflash");
            foreach (var spell in
                Player.Spellbook.Spells.Where(
                    i =>
                        i.Name.ToLower().Contains("smite") &&
                        (i.Slot == SpellSlot.Summoner1 || i.Slot == SpellSlot.Summoner2)))
            {
                Smite = spell.Slot;
            }
            Ignite = Player.GetSpellSlot("summonerdot");
            MainMenu.AddToMainMenu();
            Helper.AddNotif(string.Format("[Brian Sharp] - {0}: Loaded !", Player.ChampionName), 3000);
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
            ((Func<object>) dynamic.CreateDelegate(typeof(Func<object>)))();
        }
    }
}
