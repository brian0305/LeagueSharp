namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;

    using Valvrave_Sharp.Core;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal static class Configs
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

        #region Methods

        internal static void CreateMenu()
        {
            var evadeMenu = new Menu("Evade", "Evade Skillshot");
            {
                Config.Separator(evadeMenu, "Credit", "Credit: Evade#");
                var evadeSpells = new Menu("Spells", "Spells");
                {
                    foreach (var spell in EvadeSpellDatabase.Spells)
                    {
                        var sub = new Menu(spell.Name, string.Format("{0} ({1})", spell.Name, spell.Slot));
                        {
                            if (ObjectManager.Player.ChampionName == "Yasuo")
                            {
                                if (spell.Name == "YasuoDashWrapper")
                                {
                                    Config.Bool(sub, "ETower", "Under Tower", false);
                                }
                                else if (spell.Name == "YasuoWMovingWall")
                                {
                                    Config.Slider(sub, "WDelay", "Extra Delay", 100, 0, 150);
                                }
                            }
                            Config.Slider(sub, "DangerLevel", "If Danger Level >=", spell.DangerLevel, 1, 5);
                            if (spell.CastType == CastTypes.Target
                                && spell.ValidTargets.Contains(SpellTargets.AllyWards))
                            {
                                Config.Bool(sub, "WardJump", "Ward Jump");
                            }
                            Config.Bool(sub, "Enabled", "Enabled");
                            evadeSpells.Add(sub);
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
                    var sub = new Menu(spell.SpellName, string.Format("{0} ({1})", spell.SpellName, spell.Slot));
                    {
                        Config.Slider(sub, "DangerLevel", "Danger Level", spell.DangerValue, 1, 5);
                        Config.Bool(sub, "IsDangerous", "Is Dangerous", spell.IsDangerous);
                        Config.Bool(sub, "DisableFoW", "Disable FoW Dodging", false);
                        Config.Bool(sub, "Enabled", "Enabled", !spell.DisabledByDefault);
                        ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Add(sub);
                    }
                }
                Config.Bool(evadeMenu, "DrawStatus", "Draw Evade Status");
                Config.KeyBind(evadeMenu, "Enabled", "Enabled", Keys.K, KeyBindType.Toggle);
                Config.KeyBind(evadeMenu, "OnlyDangerous", "Dodge Only Dangerous", Keys.Space);
            }
            Program.MainMenu.Add(evadeMenu);
        }

        #endregion
    }
}