namespace BrianSharp.Evade
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
            if (ObjectManager.Player.ChampionName != "Yasuo")
            {
                return;
            }
            Spells.Add(
                new EvadeSpellData
                    {
                        Name = "YasuoDashWrapper", DangerLevel = 2, Slot = SpellSlot.E, EvadeType = EvadeTypes.Dash,
                        CastType = CastTypes.Target, MaxRange = 475, Speed = 1000, Delay = 50, FixedRange = true,
                        ValidTargets = new[] { SpellTargets.EnemyChampions, SpellTargets.EnemyMinions }
                    });
            Spells.Add(
                new EvadeSpellData
                    {
                        Name = "YasuoWMovingWall", DangerLevel = 3, Slot = SpellSlot.W, EvadeType = EvadeTypes.WindWall,
                        CastType = CastTypes.Position, MaxRange = 400, Speed = int.MaxValue, Delay = 250
                    });
        }

        #endregion
    }
}