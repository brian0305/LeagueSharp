namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Helpers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Riven : IChampionManager
    {
        #region Static Fields

        private static int lastQTick;

        #endregion

        #region Public Methods and Operators

        public void LoadSpecialSpell(SpellData spellData)
        {
            if (spellData.MenuName != "RivenQ3")
            {
                return;
            }

            CustomEvents.Unit.OnDash += (sender, args) => OnDash(sender, args, spellData);
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
        }

        #endregion

        #region Methods

        private static void OnDash(Obj_AI_Base sender, Dash.DashItem args, SpellData data)
        {
            var caster = sender as Obj_AI_Hero;

            if (caster == null || !caster.IsValid || (!caster.IsEnemy && !Configs.Debug)
                || caster.ChampionName != "Riven")
            {
                return;
            }

            if (Utils.GameTimeTickCount - lastQTick > 100)
            {
                return;
            }

            var newData = (SpellData)data.Clone();
            var endPos = args.EndPos.To3D();

            if (caster.HasBuff("RivenFengShuiEngine"))
            {
                newData.Radius += 75;
            }

            SpellDetector.AddSpell(caster, endPos, endPos, newData, null, SpellType.None, true, lastQTick);
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            var caster = sender as Obj_AI_Hero;

            if (caster == null || !caster.IsValid || (!caster.IsEnemy && !Configs.Debug)
                || caster.ChampionName != "Riven")
            {
                return;
            }

            if (args.Animation == "c49a3951")
            {
                lastQTick = Utils.GameTimeTickCount;
            }
        }

        #endregion
    }
}