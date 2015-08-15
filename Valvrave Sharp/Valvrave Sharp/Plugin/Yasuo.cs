namespace Valvrave_Sharp.Plugin
{
    using System;

    using LeagueSharp;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Wrappers;

    internal class Yasuo : AddUI
    {
        #region Constants

        private const int QRange = 550, Q2Range = 1150, QCirWidth = 275, QCirWidthMin = 250, RWidth = 400;

        #endregion

        #region Constructors and Destructors

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 500);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 1300);
            Q.SetSkillshot(GetQDelay, 20, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(GetQ2Delay, 90, 1500, false, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.High;
        }

        #endregion

        #region Properties

        private static float GetQ2Delay
        {
            get
            {
                return 0.5f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        private static float GetQDelay
        {
            get
            {
                return 0.4f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        #endregion
    }
}