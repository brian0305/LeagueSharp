namespace Valvrave_Sharp.Plugin
{
    #region

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    #endregion

    internal class Vladimir : Program
    {
        #region Constructors and Destructors

        public Vladimir()
        {
            Q = new Spell(SpellSlot.Q, 600).SetTargetted(0.25f, float.MaxValue);
            W = new Spell(SpellSlot.W, 150);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 700).SetSkillshot(
                0.25f,
                175,
                float.MaxValue,
                false,
                SkillshotType.SkillshotCircle);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Magical;
        }

        #endregion
    }
}