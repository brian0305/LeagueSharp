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

            var dir = (args.End - sender.ServerPosition).To2D().Normalized();
            var startPos = args.End - dir.Perpendicular().To3D() * (data.Range / 2f);
            var endPos = args.End + dir.Perpendicular().To3D() * (data.Range / 2f);
            SpellDetector.AddSpell(sender, startPos, endPos, data);
            SpellDetector.AddSpell(sender, endPos, startPos, data);
            spellArgs.NoProcess = true;
        }

        #endregion
    }
}