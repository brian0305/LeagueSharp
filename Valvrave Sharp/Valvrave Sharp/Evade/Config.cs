namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;

    using Valvrave_Sharp.Core;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

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

        internal static void CreateMenu(this Menu mainMenu)
        {
            var evadeMenu = new Menu("Evade", "Evade Skillshot");
            {
                evadeMenu.Separator("Credit: Evade#");
                var evadeSpells = new Menu("Spells", "Spells");
                {
                    foreach (var spell in EvadeSpellDatabase.Spells)
                    {
                        var subMenu = new Menu(spell.Name, string.Format("{0} ({1})", spell.Name, spell.Slot));
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
                            if (spell.CastType == CastTypes.Target
                                && spell.ValidTargets.Contains(SpellTargets.AllyWards))
                            {
                                subMenu.Bool("WardJump", "Ward Jump");
                            }
                            subMenu.Bool("Enabled", "Enabled");
                            evadeSpells.Add(subMenu);
                        }
                    }
                    evadeMenu.Add(evadeSpells);
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
                    var subMenu = new Menu(spell.SpellName, string.Format("{0} ({1})", spell.SpellName, spell.Slot));
                    {
                        subMenu.Slider("DangerLevel", "Danger Level", spell.DangerValue, 1, 5);
                        subMenu.Bool("IsDangerous", "Is Dangerous", spell.IsDangerous);
                        subMenu.Bool("DisableFoW", "Disable FoW Dodging", false);
                        subMenu.Bool("Enabled", "Enabled", !spell.DisabledByDefault);
                        ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(subMenu);
                    }
                }
                evadeMenu.Bool("DrawStatus", "Draw Evade Status");
                evadeMenu.KeyBind("Enabled", "Enabled", Keys.K, KeyBindType.Toggle);
                evadeMenu.KeyBind("OnlyDangerous", "Dodge Only Dangerous", Keys.Space);
            }
            mainMenu.Add(evadeMenu);
        }

        #endregion
    }
}