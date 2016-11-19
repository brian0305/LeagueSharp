namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Azir : IChampionManager
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
            SpellDetector.OnProcessSpell += AzirR;
        }

        #endregion

        #region Methods

        private static void AzirR(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "AzirR")
            {
                return;
            }

            var newData = (SpellData)data.Clone();
            newData.Radius = data.Radius * new[] { 4, 5, 6 }[((Obj_AI_Hero)sender).GetSpell(data.Slot).Level - 1];
            //spellArgs.NewData = newData;
        }

        #endregion
    }
}