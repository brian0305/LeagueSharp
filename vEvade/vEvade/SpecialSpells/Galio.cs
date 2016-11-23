namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
    using vEvade.Helpers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Galio : IChampionManager
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
            SpellDetector.OnCreateSpell += GalioE;
        }

        #endregion

        #region Methods

        private static void GalioE(Obj_AI_Base sender, MissileClient missile, SpellData data, SpellArgs spellArgs)
        {
            if (data.MenuName != "GalioE")
            {
                return;
            }

            var hero =
                HeroManager.AllHeroes.FirstOrDefault(
                    i => i.ChampionName == data.ChampName && (i.IsEnemy || Configs.Debug));

            if (hero != null)
            {
                var spell =
                    Evade.SpellsDetected.Values.FirstOrDefault(
                        i =>
                        i.Data.MenuName == data.MenuName && i.Unit.NetworkId == hero.NetworkId
                        && i.MissileObject == null);

                if (spell != null)
                {
                    Evade.SpellsDetected.Remove(spell.SpellId);
                }

                SpellDetector.AddSpell(hero, missile.StartPosition, missile.EndPosition, data, missile);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}