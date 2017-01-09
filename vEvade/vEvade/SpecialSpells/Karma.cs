namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;

    using vEvade.Core;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Karma : IChampionManager
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
            SpellDetector.OnProcessSpell += KarmaQ;
        }

        #endregion

        #region Methods

        private static void KarmaQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "KarmaQ" || !sender.HasBuff("KarmaMantra"))
            {
                return;
            }

            SpellData mantraData;

            if (Evade.OnProcessSpells.TryGetValue("KarmaQMantra", out mantraData))
            {
                SpellDetector.AddSpell(sender, sender.ServerPosition, args.End, mantraData);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}