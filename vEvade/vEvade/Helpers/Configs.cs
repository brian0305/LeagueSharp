namespace vEvade.Helpers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
    using vEvade.EvadeSpells;
    using vEvade.SpecialSpells;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    internal static class Configs
    {
        #region Constants

        public const int CrossingTime = 250;

        public const int EvadePointChangeTime = 300;

        public const int EvadingFirstTime = 250;

        public const int EvadingRouteChangeTime = 250;

        public const int EvadingSecondTime = 80;

        public const int ExtraSpellRadius = 9;

        public const int ExtraSpellRange = 20;

        public const int GridSize = 10;

        public const int PathFindingInnerDistance = 35;

        public const int PathFindingOuterDistance = 60;

        #endregion

        #region Static Fields

        public static bool Debug;

        public static Menu Menu;

        private static readonly Dictionary<string, IChampionManager> ChampionManagers =
            new Dictionary<string, IChampionManager>();

        #endregion

        #region Public Properties

        public static int CheckBlock => Menu.Item("CheckBlock").GetValue<StringList>().SelectedIndex;

        public static bool CheckCollision => Menu.Item("CheckCollision").GetValue<bool>();

        public static bool CheckHp => Menu.Item("CheckHp").GetValue<bool>();

        public static bool DodgeCircle => Menu.Item("DodgeCircle").GetValue<bool>();

        public static bool DodgeCone => Menu.Item("DodgeCone").GetValue<bool>();

        public static bool DodgeDangerous => Menu.Item("DodgeDangerous").GetValue<KeyBind>().Active;

        public static int DodgeFoW => Menu.Item("DodgeFoW").GetValue<StringList>().SelectedIndex;

        public static bool DodgeLine => Menu.Item("DodgeLine").GetValue<bool>();

        public static bool DodgeTrap => Menu.Item("DodgeTrap").GetValue<bool>();

        public static bool DrawSpells => Menu.Item("DrawSpells").GetValue<bool>();

        public static bool DrawStatus => Menu.Item("DrawStatus").GetValue<bool>();

        public static bool Enabled => Menu.Item("Enabled").GetValue<KeyBind>().Active;

        #endregion

        #region Public Methods and Operators

        public static void CreateMenu()
        {
            Menu = new Menu("vEvade", "vEvade", true);
            Menu.AddToMainMenu();
            LoadSpecialSpellPlugins();

            var spells = new Menu("Spells", "Spells");

            foreach (var hero in HeroManager.AllHeroes.Where(i => i.IsEnemy || Debug))
            {
                foreach (var spell in
                    SpellDatabase.Spells.Where(
                        i =>
                        !Evade.OnProcessSpells.ContainsKey(i.SpellName)
                        && (i.IsSummoner || i.ChampName == hero.ChampionName)))
                {
                    if (spell.IsSummoner && hero.GetSpellSlot(spell.SpellName) != SpellSlot.Summoner1
                        && hero.GetSpellSlot(spell.SpellName) != SpellSlot.Summoner2)
                    {
                        continue;
                    }

                    Evade.OnProcessSpells.Add(spell.SpellName, spell);

                    foreach (var name in spell.ExtraSpellNames)
                    {
                        Evade.OnProcessSpells.Add(name, spell);
                    }

                    if (!string.IsNullOrEmpty(spell.MissileName))
                    {
                        Evade.OnMissileSpells.Add(spell.MissileName, spell);
                    }

                    foreach (var name in spell.ExtraMissileNames)
                    {
                        Evade.OnMissileSpells.Add(name, spell);
                    }

                    if (!string.IsNullOrEmpty(spell.TrapName))
                    {
                        Evade.OnTrapSpells.Add(spell.TrapName, spell);
                    }

                    LoadSpecialSpell(spell);

                    var txt = "S_" + spell.MenuName;
                    var subMenu =
                        new Menu(spell.IsSummoner ? spell.SpellName : spell.ChampName + " (" + spell.Slot + ")", txt);
                    subMenu.AddItem(
                        new MenuItem(txt + "_DangerLvl", "Danger Level").SetValue(new Slider(spell.DangerValue, 1, 5)));
                    subMenu.AddItem(new MenuItem(txt + "_IsDangerous", "Is Dangerous").SetValue(spell.IsDangerous));
                    subMenu.AddItem(new MenuItem(txt + "_IgnoreHp", "Ignore If Hp >"))
                        .SetValue(new Slider(!spell.IsDangerous ? 65 : 80, 1));
                    subMenu.AddItem(new MenuItem(txt + "_Draw", "Draw").SetValue(true));
                    subMenu.AddItem(new MenuItem(txt + "_Enabled", "Enabled").SetValue(!spell.DisabledByDefault))
                        .SetTooltip(spell.MenuName);
                    spells.AddSubMenu(subMenu);
                }
            }

            Menu.AddSubMenu(spells);

            var evadeSpells = new Menu("Evade Spells", "EvadeSpells");

            foreach (var spell in EvadeSpellDatabase.Spells)
            {
                var txt = "ES_" + spell.MenuName;
                var subMenu = new Menu(spell.MenuName, txt);
                subMenu.AddItem(
                    new MenuItem(txt + "_DangerLvl", "Danger Level").SetValue(new Slider(spell.DangerLevel, 1, 5)));

                if (spell.IsTargetted && spell.ValidTargets.Contains(SpellValidTargets.AllyWards))
                {
                    subMenu.AddItem(new MenuItem(txt + "_WardJump", "Ward Jump").SetValue(false));
                }

                subMenu.AddItem(new MenuItem(txt + "_Enabled", "Enabled").SetValue(true));
                evadeSpells.AddSubMenu(subMenu);
            }

            Menu.AddSubMenu(evadeSpells);

            var shieldAlly = new Menu("Shield Ally", "ShieldAlly");

            foreach (var ally in HeroManager.Allies.Where(i => !i.IsMe))
            {
                shieldAlly.AddItem(new MenuItem("SA_" + ally.ChampionName, ally.ChampionName).SetValue(false));
            }

            Menu.AddSubMenu(shieldAlly);

            var misc = new Menu("Misc", "Misc");
            misc.AddItem(new MenuItem("CheckCollision", "Check Collision").SetValue(false));
            misc.AddItem(new MenuItem("CheckHp", "Check Player Hp").SetValue(false));
            misc.AddItem(
                new MenuItem("CheckBlock", "Block Cast While Dodge").SetValue(
                    new StringList(new[] { "No", "Only Dangerous", "Always" }, 1)));
            misc.AddItem(
                new MenuItem("DodgeFoW", "Dodge FoW Spells").SetValue(
                    new StringList(new[] { "Off", "Track", "Dodge" }, 2)));
            misc.AddItem(new MenuItem("DodgeLine", "Dodge Line Spells").SetValue(true));
            misc.AddItem(new MenuItem("DodgeCircle", "Dodge Circle Spells").SetValue(false));
            misc.AddItem(new MenuItem("DodgeCone", "Dodge Cone Spells").SetValue(true));
            misc.AddItem(new MenuItem("DodgeTrap", "Dodge Traps").SetValue(false));
            Menu.AddSubMenu(misc);

            var draw = new Menu("Draw", "Draw");
            draw.AddItem(new MenuItem("DrawSpells", "Draw Spells").SetValue(true));
            draw.AddItem(new MenuItem("DrawStatus", "Draw Status").SetValue(true));
            Menu.AddSubMenu(draw);

            Menu.AddItem(new MenuItem("Enabled", "Enabled").SetValue(new KeyBind('K', KeyBindType.Toggle, true)))
                .Permashow();
            Menu.AddItem(
                new MenuItem("DodgeDangerous", "Dodge Only Dangerous").SetValue(new KeyBind(32, KeyBindType.Press)))
                .Permashow();
        }

        #endregion

        #region Methods

        private static void LoadSpecialSpell(SpellData spell)
        {
            if (ChampionManagers.ContainsKey(spell.ChampName))
            {
                ChampionManagers[spell.ChampName].LoadSpecialSpell(spell);
            }

            ChampionManagers["AllChampions"].LoadSpecialSpell(spell);
        }

        private static void LoadSpecialSpellPlugins()
        {
            ChampionManagers.Add("AllChampions", new AllChampions());

            foreach (var hero in HeroManager.AllHeroes.Where(i => i.IsEnemy || Debug))
            {
                var plugin =
                    Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .FirstOrDefault(
                            i => i.IsClass && i.Namespace == "vEvade.SpecialSpells" && i.Name == hero.ChampionName);

                if (plugin != null && !ChampionManagers.ContainsKey(hero.ChampionName))
                {
                    ChampionManagers.Add(hero.ChampionName, (IChampionManager)NewInstance(plugin));
                }
            }
        }

        private static object NewInstance(Type type)
        {
            var target = type.GetConstructor(Type.EmptyTypes);
            var dynamic = new DynamicMethod(string.Empty, type, new Type[0], target.DeclaringType);
            var il = dynamic.GetILGenerator();
            il.DeclareLocal(target.DeclaringType);
            il.Emit(OpCodes.Newobj, target);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            var method = (Func<object>)dynamic.CreateDelegate(typeof(Func<object>));

            return method();
        }

        #endregion
    }
}