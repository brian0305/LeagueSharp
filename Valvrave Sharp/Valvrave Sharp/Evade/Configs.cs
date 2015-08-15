namespace Valvrave_Sharp.Evade
{
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp.SDK.Core;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal static class Config
    {
        #region Constants

        public const int DiagonalEvadePointsCount = 7;

        public const int DiagonalEvadePointsStep = 20;

        public const int EvadingFirstTimeOffset = 250;

        public const int EvadingSecondTimeOffset = 0;

        public const int ExtraEvadeDistance = 15;

        public const int GridSize = 10;

        public const int SkillShotsExtraRadius = 9;

        public const int SkillShotsExtraRange = 20;

        #endregion

        #region Public Methods and Operators

        public static void CreateMenu()
        {
            var evadeMenu = new Menu("Evade", "Evade");
            {
                AddUI.Separator(evadeMenu, "Credit", "Credit: Evade#");
                var evadeSpells = new Menu("Spells", "Spells");
                {
                    foreach (var spell in EvadeSpellDatabase.Spells)
                    {
                        var sub = new Menu(spell.Name, spell.Name + " (" + spell.Slot + ")");
                        {
                            if (Program.Player.ChampionName == "Yasuo")
                            {
                                if (spell.Name == "YasuoDashWrapper")
                                {
                                    AddUI.Bool(sub, "ETower", "Under Tower", false);
                                }
                                else if (spell.Name == "YasuoWMovingWall")
                                {
                                    AddUI.Slider(sub, "WDelay", "Extra Delay", 100, 0, 150);
                                }
                            }
                            AddUI.Slider(sub, "DangerLevel", "If Danger Level >=", spell.DangerLevel, 1, 5);
                            if (spell.CastType == CastTypes.Target
                                && spell.ValidTargets.Contains(SpellTargets.AllyWards))
                            {
                                AddUI.Bool(sub, "WardJump", "Ward Jump");
                            }
                            AddUI.Bool(sub, "Enabled", "Enabled", false);
                            evadeSpells.Add(sub);
                        }
                    }
                    evadeMenu.Add(evadeSpells);
                }
                foreach (var hero in
                    GameObjects.EnemyHeroes.Where(i => SpellDatabase.Spells.Any(a => a.ChampionName == i.ChampionName)))
                {
                    evadeMenu.Add(new Menu(hero.ChampionName, "-> " + hero.ChampionName));
                }
                foreach (var spell in
                    SpellDatabase.Spells.Where(i => GameObjects.EnemyHeroes.Any(a => a.ChampionName == i.ChampionName)))
                {
                    var sub = new Menu(spell.SpellName, spell.SpellName + " (" + spell.Slot + ")");
                    {
                        AddUI.Slider(sub, "DangerLevel", "Danger Level", spell.DangerValue, 1, 5);
                        AddUI.Bool(sub, "IsDangerous", "Is Dangerous", spell.IsDangerous);
                        AddUI.Bool(sub, "DisableFoW", "Disable FoW Dodging", false);
                        AddUI.Bool(sub, "Enabled", "Enabled", !spell.DisabledByDefault);
                        ((Menu)evadeMenu[spell.ChampionName]).Add(sub);
                    }
                }
                AddUI.KeyBind(evadeMenu, "OnlyDangerous", "Dodge Only Dangerous", Keys.Space);
            }
            Program.MainMenu.Add(evadeMenu);
        }

        #endregion
    }
}