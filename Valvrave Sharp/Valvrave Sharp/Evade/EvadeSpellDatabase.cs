namespace Valvrave_Sharp.Evade
{
    using System.Collections.Generic;

    using LeagueSharp;

    internal class EvadeSpellDatabase
    {
        #region Static Fields

        public static List<EvadeSpellData> Spells = new List<EvadeSpellData>();

        #endregion

        #region Constructors and Destructors

        static EvadeSpellDatabase()
        {
            switch (ObjectManager.Player.ChampionName)
            {
                case "Yasuo":
                    Spells.Add(
                        new EvadeSpellData
                            {
                                Name = "YasuoDashWrapper", CheckBuffName = "YasuoDashWrapper", DangerLevel = 2,
                                Slot = SpellSlot.E, EvadeType = EvadeTypes.Dash, CastType = CastTypes.Target,
                                MaxRange = 475, Speed = 1000, Delay = 50, FixedRange = true, UnderTower = true,
                                ValidTargets = new[] { SpellTargets.EnemyChampions, SpellTargets.EnemyMinions }
                            });
                    Spells.Add(
                        new EvadeSpellData
                            {
                                Name = "YasuoWMovingWall", DangerLevel = 3, Slot = SpellSlot.W,
                                EvadeType = EvadeTypes.WindWall, CastType = CastTypes.Position, MaxRange = 400,
                                Speed = int.MaxValue, Delay = 250, ExtraDelay = true
                            });
                    break;
                case "Zed":
                    Spells.Add(
                        new EvadeSpellData
                            {
                                Name = "ZedRShadow", CheckSpellName = "zedr2", DangerLevel = 4, Slot = SpellSlot.R,
                                EvadeType = EvadeTypes.Blink, CastType = CastTypes.Self, MaxRange = 25000,
                                Speed = int.MaxValue, Delay = 50, ExtraDelay = true
                            });
                    break;
            }
        }

        #endregion
    }
}