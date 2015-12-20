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
            EvadeSpellData spell;

            #region Champion Dashes

            #region Yasuo

            if (ObjectManager.Player.ChampionName == "Yasuo")
            {
                spell = new DashData("Yasuo E", SpellSlot.E, 475, true, 100, 1400, 2)
                            {
                                ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions },
                                UnderTower = true
                            };
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Champion Blinks

            #region Zed

            if (ObjectManager.Player.ChampionName == "Zed")
            {
                spell = new BlinkData("Zed W2", SpellSlot.W, 20000, 100, 3)
                            { CheckSpellName = "zedw2", SelfCast = true };
                Spells.Add(spell);
                spell = new BlinkData("Zed R2", SpellSlot.R, 20000, 100, 4)
                            { CheckSpellName = "zedr2", SelfCast = true };
                Spells.Add(spell);
            }

            #endregion

            #endregion

            #region Champion Invulnerabilities

            #region Yasuo

            if (ObjectManager.Player.ChampionName == "Yasuo")
            {
                spell = new InvulnerabilityData("Yasuo W", SpellSlot.W, 250, 3) { ExtraDelay = true };
                Spells.Add(spell);
            }

            #endregion

            #endregion
        }

        #endregion
    }
}