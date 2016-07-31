namespace Valvrave_Sharp.Evade
{
    #region

    using System.Collections.Generic;

    using LeagueSharp;

    #endregion

    internal static class EvadeSpellDatabase
    {
        #region Static Fields

        internal static List<EvadeSpellData> Spells = new List<EvadeSpellData>();

        #endregion

        #region Methods

        internal static void Init()
        {
            #region Champion Dashes

            #region Yasuo

            if (Program.Player.ChampionName == "Yasuo")
            {
                Spells.Add(
                    new DashData("Yasuo E", SpellSlot.E, 475, true, 100, 1200, 2)
                        {
                            ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions },
                            CheckBuffName = "YasuoDashWrapper", UnderTower = true
                        });
            }

            #endregion

            #endregion

            #region Champion Blinks

            #region Zed

            if (Program.Player.ChampionName == "Zed")
            {
                Spells.Add(
                    new BlinkData("Zed W2", SpellSlot.W, 1, 100, 2)
                        {
                            CheckSpellName = "zedw2", ValidTargets = new[] { SpellValidTargets.AllyObjects },
                            RequireBuff = "zedwshadowbuff", SelfCast = true, UnderTower = true
                        });
                Spells.Add(
                    new BlinkData("Zed R1", SpellSlot.R, 625, 100, 4)
                        { CheckSpellName = "zedr", ValidTargets = new[] { SpellValidTargets.EnemyChampions } });
                Spells.Add(
                    new BlinkData("Zed R2", SpellSlot.R, 1, 100, 3)
                        {
                            CheckSpellName = "zedr2", ValidTargets = new[] { SpellValidTargets.AllyObjects },
                            RequireBuff = "zedrshadowbuff", SelfCast = true, UnderTower = true
                        });
            }

            #endregion

            #endregion

            #region Champion Invulnerabilities

            #region Vladimir

            if (Program.Player.ChampionName == "Vladimir")
            {
                Spells.Add(new InvulnerabilityData("Vladimir W", SpellSlot.W, 100, 3) { SelfCast = true });
            }

            #endregion

            #endregion

            #region Champion Shields

            #region Yasuo

            if (Program.Player.ChampionName == "Yasuo")
            {
                Spells.Add(
                    new ShieldData("Yasuo W", SpellSlot.W, 250, 3)
                        { Range = 400, CanShieldAllies = true, RequireMissilePos = true, IsYasuoWall = true });
            }

            #endregion

            #endregion
        }

        #endregion
    }
}