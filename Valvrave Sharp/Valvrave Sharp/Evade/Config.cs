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

        public const int DiagonalEvadePointsCount = 7;

        public const int DiagonalEvadePointsStep = 20;

        public const int EvadingFirstTimeOffset = 250;

        public const int EvadingSecondTimeOffset = 80;

        public const int ExtraEvadeDistance = 15;

        public const int GridSize = 10;

        public const int SkillShotsExtraRadius = 9;

        public const int SkillShotsExtraRange = 20;

        #endregion

        #region Public Methods and Operators

        public static void CreateMenu(Menu mainMenu)
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
                        subMenu.Bool("DisableFoW", "Disable FoW Dodging", false);
                        subMenu.Bool("Draw", "Draw");
                        subMenu.Bool("Enabled", "Enabled", !spell.DisabledByDefault);
                    }
                }
                var drawMenu = evadeMenu.Add(new Menu("Draw", "Draw"));
                {
                    drawMenu.Bool("Skillshot", "Skillshot");
                    drawMenu.Bool("Status", "Evade Status");
                }
                evadeMenu.KeyBind("Enabled", "Enabled", Keys.K, KeyBindType.Toggle);
                evadeMenu.KeyBind("OnlyDangerous", "Dodge Only Dangerous", Keys.Space);
            }
        }

        #endregion
    }
}