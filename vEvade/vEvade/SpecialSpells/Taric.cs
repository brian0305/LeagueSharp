namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Taric : IChampionManager
    {
        #region Static Fields

        private static bool init;

        #endregion

        #region Public Methods and Operators

        public void LoadSpecialSpell(SpellData spellData)
        {
            if (init)
            {
                return;
            }

            init = true;
            SpellDetector.OnProcessSpell += TaricE;
        }

        #endregion

        #region Methods

        private static void TaricE(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "TaricE")
            {
                return;
            }

            foreach (var hero in
                HeroManager.AllHeroes.Where(i => !i.IsDead && i.IsVisible && i.Team == sender.Team))
            {
                var buff = hero.GetBuff("taricwleashactive");

                if (buff != null && buff.Caster.NetworkId == sender.NetworkId)
                {
                    SpellDetector.AddSpell(hero, hero.ServerPosition, args.End, data);
                }
            }
        }

        #endregion
    }
}