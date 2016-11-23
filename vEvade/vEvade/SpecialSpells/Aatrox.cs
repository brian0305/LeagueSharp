namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Aatrox : IChampionManager
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
            SpellDetector.OnProcessSpell += AatroxE;
        }

        #endregion

        #region Methods

        private static void AatroxE(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "AatroxE")
            {
                return;
            }

            var startPos = sender.ServerPosition.To2D();
            var endPos = args.End.To2D();
            var dir = (endPos - startPos).Normalized().Perpendicular();
            SpellDetector.AddSpell(sender, startPos + dir * data.Radius, endPos, data);
            SpellDetector.AddSpell(sender, startPos - dir * data.Radius, endPos, data);
            spellArgs.NoProcess = true;
        }

        #endregion
    }
}