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

        public const int CrossingTimeOffset = 250;

        public const int DiagonalEvadePointsCount = 7;

        public const int DiagonalEvadePointsStep = 20;

        public const int EvadePointChangeInterval = 300;

        public const int EvadingFirstTimeOffset = 250;

        public const int EvadingRouteChangeTimeOffset = 250;

        public const int EvadingSecondTimeOffset = 80;

        public const int ExtraEvadeDistance = 15;

        public const int GridSize = 10;

        public const int PathFindingDistance = 60;

        public const int PathFindingDistance2 = 35;

        public const int SpellExtraRadius = 9;

        public const int SpellExtraRange = 20;

        #endregion

        #region Static Fields

        public static bool Debug = false;

        public static Menu Menu;

        private static readonly Dictionary<string, IChampionManager> ChampionManagers =
            new Dictionary<string, IChampionManager>();

        #endregion

        #region Public Methods and Operators

        public static void CreateMenu()
        {
            Menu = new Menu("vEvade", "vEvade", true);
            LoadSpecialSpellPlugins();

            var spells = new Menu("Spells", "Spells");

            foreach (var hero in HeroManager.AllHeroes.Where(i => i.IsEnemy || Debug))
            {
                foreach (var spell in
                    SpellDatabase.Spells.Where(
                        i =>
                        !Evade.OnProcessSpells.ContainsKey(i.SpellName)
                        && (i.ChampName == hero.ChampionName || i.IsSummoner)))
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

                    if (spell.MissileName != "")
                    {
                        Evade.OnMissileSpells.Add(spell.MissileName, spell);
                    }

                    foreach (var name in spell.ExtraMissileNames)
                    {
                        Evade.OnMissileSpells.Add(name, spell);
                    }

                    if (spell.TrapName != "")
                    {
                        Evade.OnTrapSpells.Add(spell.TrapName, spell);
                    }

                    LoadSpecialSpell(spell);

                    var subMenu =
                        new Menu(
                            spell.IsSummoner ? spell.SpellName : spell.ChampName + " (" + spell.Slot + ")",
                            "S_" + spell.MenuName);
                    subMenu.AddItem(
                        new MenuItem("S_" + spell.MenuName + "_DangerLvl", "Danger Level").SetValue(
                            new Slider(spell.DangerValue, 1, 5)));
                    subMenu.AddItem(
                        new MenuItem("S_" + spell.MenuName + "_IsDangerous", "Is Dangerous").SetValue(spell.IsDangerous));
                    subMenu.AddItem(new MenuItem("S_" + spell.MenuName + "_IgnoreHp", "Ignore If Hp >="))
                        .SetValue(new Slider(!spell.IsDangerous ? 65 : 80, 1));
                    subMenu.AddItem(new MenuItem("S_" + spell.MenuName + "_Draw", "Draw").SetValue(true));
                    subMenu.AddItem(new MenuItem("S_" + spell.MenuName + "_Enabled", "Enabled").SetValue(true))
                        .SetTooltip(spell.MenuName);
                    spells.AddSubMenu(subMenu);
                }
            }

            Menu.AddSubMenu(spells);

            var evadeSpells = new Menu("Evade Spells", "EvadeSpells");

            foreach (var spell in EvadeSpellDatabase.Spells)
            {
                var subMenu = new Menu(spell.MenuName, "ES_" + spell.MenuName);
                subMenu.AddItem(
                    new MenuItem("ES_" + spell.MenuName + "_DangerLvl", "Danger Level").SetValue(
                        new Slider(spell.DangerLevel, 1, 5)));

                if (spell.IsTargetted && spell.ValidTargets.Contains(SpellValidTargets.AllyWards))
                {
                    subMenu.AddItem(new MenuItem("ES_" + spell.MenuName + "_WardJump", "Ward Jump").SetValue(true));
                }

                subMenu.AddItem(new MenuItem("ES_" + spell.MenuName + "_Enabled", "Enabled").SetValue(true));
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
            misc.AddItem(new MenuItem("CheckCollision", "Check Collision").SetValue(true));
            misc.AddItem(new MenuItem("CheckHp", "Check Player Hp").SetValue(true));
            misc.AddItem(new MenuItem("DodgeFoW", "Dodge FoW Spells").SetValue(true));
            misc.AddItem(new MenuItem("DodgeLine", "Dodge Line Spells").SetValue(true));
            misc.AddItem(new MenuItem("DodgeCircle", "Dodge Circle Spells").SetValue(true));
            misc.AddItem(new MenuItem("DodgeCone", "Dodge Cone Spells").SetValue(true));
            misc.AddItem(new MenuItem("DodgeTrap", "Dodge Traps").SetValue(true));
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

            Menu.AddToMainMenu();
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