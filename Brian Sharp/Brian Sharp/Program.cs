using System;
using System.Linq;
using System.Reflection.Emit;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
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

        private static void OnCreateObjMissile(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<Obj_SpellMissile>())
            {
                return;
            }
            var missile = (Obj_SpellMissile) sender;
            if (!missile.SpellCaster.IsMe || !missile.SpellCaster.IsValid<Obj_AI_Hero>() || missile.SData.IsAutoAttack())
            {
                return;
            }
            Game.PrintChat(
                "(O) [{0}]{1}: Start[{2}]/End[{3}]/R[{4}|{5}|{6}]/D[{7}]/W[{8}|{9}]/S[{10}]",
                ((Obj_AI_Hero) missile.SpellCaster).ChampionName, missile.SData.Name, missile.StartPosition,
                missile.EndPosition, missile.StartPosition.Distance(missile.EndPosition), missile.SData.CastRange,
                missile.SData.CastRangeDisplayOverride, missile.SData.DelayTotalTimePercent, missile.SData.CastRadius,
                missile.SData.LineWidth, missile.SData.MissileSpeed);
        }

        private static void OnDeleteObjMissile(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<Obj_SpellMissile>())
            {
                return;
            }
            var missile = (Obj_SpellMissile) sender;
            if (!missile.SpellCaster.IsMe || !missile.SpellCaster.IsValid<Obj_AI_Hero>() || missile.SData.IsAutoAttack())
            {
                return;
            }
            Game.PrintChat("{0}: {1}", missile.SData.Name, Player.Distance(missile.Position));
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || !sender.IsValid<Obj_AI_Hero>() || args.SData.IsAutoAttack())
            {
                return;
            }
            Game.PrintChat(
                "(S) [{0}]{1}: Start[{2}]/End[{3}]/R[{4}|{5}|{6}]/D[{7}]/W[{8}|{9}]/S[{10}]",
                ((Obj_AI_Hero) sender).ChampionName, args.SData.Name, args.Start, args.End,
                args.Start.Distance(args.End), args.SData.CastRange, args.SData.CastRangeDisplayOverride,
                args.SData.DelayTotalTimePercent, args.SData.CastRadius, args.SData.LineWidth, args.SData.MissileSpeed);
        }

        private static void OnStart(EventArgs args)
        {
            //GameObject.OnCreate += OnCreateObjMissile;
            //GameObject.OnDelete += OnDeleteObjMissile;
            //Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            var plugin = Type.GetType("BrianSharp.Plugin." + Player.ChampionName);
            if (plugin == null)
            {
                Game.PrintChat("[Brian Sharp] - {0}: Not Load !", Player.ChampionName);
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
            Helper.AddItem(
                MainMenu.SubMenu(Player.ChampionName + "_Plugin").SubMenu("Misc"), "UsePacket", "Use Packet To Cast");
            Tiamat = ItemData.Tiamat_Melee_Only.GetItem();
            Hydra = ItemData.Ravenous_Hydra_Melee_Only.GetItem();
            Youmuu = ItemData.Youmuus_Ghostblade.GetItem();
            Zhonya = ItemData.Zhonyas_Hourglass.GetItem();
            Seraph = ItemData.Seraphs_Embrace.GetItem();
            Sheen = ItemData.Sheen.GetItem();
            Iceborn = ItemData.Iceborn_Gauntlet.GetItem();
            Trinity = ItemData.Trinity_Force.GetItem();
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
            Game.PrintChat("[Brian Sharp] - {0}: Loaded !", Player.ChampionName);
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