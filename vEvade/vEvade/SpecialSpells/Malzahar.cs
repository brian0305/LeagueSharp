namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Malzahar : IChampionManager
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
            SpellDetector.OnProcessSpell += MalzaharQ;
        }

        #endregion

        #region Methods

        private static void MalzaharQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "MalzaharQ")
            {
                return;
            }

            var end = args.End.To2D();
            var dir = (end - sender.ServerPosition.To2D()).Normalized().Perpendicular() * (data.Range / 2f);
            var startPos = end - dir;
            var endPos = end + dir;
            SpellDetector.AddSpell(sender, startPos, endPos, data);
            SpellDetector.AddSpell(sender, endPos, startPos, data);
            spellArgs.NoProcess = true;
        }

        #endregion
    }
}