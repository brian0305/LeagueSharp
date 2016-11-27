namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;

    using vEvade.Helpers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class RekSai : IChampionManager
    {
        #region Public Methods and Operators

        public void LoadSpecialSpell(SpellData spellData)
        {
            if (spellData.MenuName != "RekSaiW")
            {
                return;
            }

            Obj_AI_Base.OnPlayAnimation += (sender, args) => OnPlayAnimation(sender, args, spellData);
        }

        #endregion

        #region Methods

        private static void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args, SpellData data)
        {
            var caster = sender as Obj_AI_Hero;

            if (caster == null || !caster.IsValid || (!caster.IsEnemy && !Configs.Debug)
                || caster.ChampionName != data.ChampName)
            {
                return;
            }

            if ((args.Animation == "c0362dea" || args.Animation == "c6352f63")
                && !ObjectManager.Player.HasBuff("reksaiknockupimmune"))
            {
                SpellDetector.AddSpell(caster, caster.ServerPosition, caster.ServerPosition, data);
            }
        }

        #endregion
    }
}