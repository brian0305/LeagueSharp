namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Olaf : IChampionManager
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
            SpellDetector.OnProcessSpell += OlafQ;
        }

        #endregion

        #region Methods

        private static void OlafQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "OlafQ")
            {
                return;
            }

            var startPos = sender.ServerPosition.To2D();
            var endPos = args.End.To2D().Extend(startPos, -50);
            SpellDetector.AddSpell(sender, startPos, endPos, data);
            spellArgs.NoProcess = true;
        }

        #endregion
    }
}