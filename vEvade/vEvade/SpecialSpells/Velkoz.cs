namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Velkoz : IChampionManager
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
            SpellDetector.OnProcessSpell += VelkozW;
        }

        #endregion

        #region Methods

        private static void VelkozW(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "VelkozW")
            {
                return;
            }

            SpellDetector.AddSpell(sender, sender.ServerPosition.Extend(args.End, -70), args.End, data);
            spellArgs.NoProcess = true;
        }

        #endregion
    }
}