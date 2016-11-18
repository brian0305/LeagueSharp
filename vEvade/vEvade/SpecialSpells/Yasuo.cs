namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Yasuo : IChampionManager
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
            SpellDetector.OnProcessSpell += YasuoQ;
        }

        #endregion

        #region Methods

        private static void YasuoQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "YasuoQ")
            {
                return;
            }

            var delay = (int)(sender.Spellbook.CastTime - Game.Time) * 1000;

            if (delay <= 0)
            {
                return;
            }

            var newData = (SpellData)data.Clone();
            newData.Delay = delay;
            spellArgs.NewData = newData;
        }

        #endregion
    }
}