﻿namespace Valvrave_Sharp.Evade
{
    #region

    using System;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp.SDKEx;
    using LeagueSharp.SDKEx.Enumerations;

    using Valvrave_Sharp.Core;

    using Menu = LeagueSharp.SDKEx.UI.Menu;

    #endregion

    internal static class Config
    {
        #region Constants

        internal const int DiagonalEvadePointsCount = 7;

        internal const int DiagonalEvadePointsStep = 20;

        internal const int EvadingFirstTimeOffset = 250;

        internal const int EvadingSecondTimeOffset = 80;

        internal const int ExtraEvadeDistance = 15;

        internal const int GridSize = 10;

        internal const int SkillShotsExtraRadius = 9;

        internal const int SkillShotsExtraRange = 20;

        #endregion

        #region Methods

        internal static void CreateMenu(Menu mainMenu)
        {
            var evadeMenu = mainMenu.Add(new Menu("Evade", "Evade Skillshot"));
            {
                evadeMenu.Separator("Credit: Evade#");
                var evadeSpells = evadeMenu.Add(new Menu("Spells", "Spells"));
                {
                    foreach (var spell in EvadeSpellDatabase.Spells)
                    {
                        var subMenu = evadeSpells.Add(new Menu(spell.Name, spell.Name));
                        {
                            if (spell.UnderTower)
                            {
                                subMenu.Bool(spell.Slot + "Tower", "Under Tower", false);
                            }
                            if (spell.ExtraDelay)
                            {
                                subMenu.Slider(spell.Slot + "Delay", "Extra Delay", 100, 0, 150);
                            }
                            subMenu.Slider("DangerLevel", "If Danger Level >=", spell.DangerLevel, 1, 5);
                            if (spell.IsTargetted && spell.ValidTargets.Contains(SpellValidTargets.AllyWards))
                            {
                                subMenu.Bool("WardJump", "Ward Jump");
                            }
                            subMenu.Bool("Enabled", "Enabled");
                        }
                    }
                }
                foreach (var hero in
                    GameObjects.EnemyHeroes.Where(
                        i =>
                        SpellDatabase.Spells.Any(
                            a =>
                            string.Equals(a.ChampionName, i.ChampionName, StringComparison.InvariantCultureIgnoreCase)))
                    )
                {
                    evadeMenu.Add(new Menu(hero.ChampionName.ToLowerInvariant(), "-> " + hero.ChampionName));
                }
                foreach (var spell in
                    SpellDatabase.Spells.Where(
                        i =>
                        GameObjects.EnemyHeroes.Any(
                            a =>
                            string.Equals(a.ChampionName, i.ChampionName, StringComparison.InvariantCultureIgnoreCase)))
                    )
                {
                    var subMenu =
                        ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(
                            new Menu(spell.SpellName, $"{spell.SpellName} ({spell.Slot})"));
                    {
                        subMenu.Slider("DangerLevel", "Danger Level", spell.DangerValue, 1, 5);
                        subMenu.Bool("IsDangerous", "Is Dangerous", spell.IsDangerous);
                        if (!spell.DisableFowDetection)
                        {
                            subMenu.Bool("DisableFoW", "Disable FoW Dodging", false);
                        }
                        subMenu.Bool("Draw", "Draw");
                        subMenu.Bool("Enabled", "Enabled", !spell.DisabledByDefault);
                    }
                }
                var drawMenu = evadeMenu.Add(new Menu("Draw", "Draw"));
                {
                    drawMenu.Bool("Skillshot", "Skillshot");
                    drawMenu.Bool("Status", "Evade Status");
                }
                evadeMenu.Bool("DisableFoW", "Disable All FoW Dodging", false);
                evadeMenu.KeyBind("Enabled", "Enabled", Keys.K, KeyBindType.Toggle);
                evadeMenu.KeyBind("OnlyDangerous", "Dodge Only Dangerous", Keys.Space);
            }
        }

        #endregion
    }
}