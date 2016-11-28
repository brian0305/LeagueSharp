namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;

    using vEvade.Core;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Diana : IChampionManager
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
            SpellDetector.OnProcessSpell += DianaQ;
        }

        #endregion

        #region Methods

        private static void DianaQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "DianaQCircle")
            {
                return;
            }

            SpellData arcData;

            if (Evade.OnProcessSpells.TryGetValue("DianaQArc", out arcData))
            {
                SpellDetector.AddSpell(sender, sender.ServerPosition, args.End, arcData);
            }
        }

        #endregion
    }
}