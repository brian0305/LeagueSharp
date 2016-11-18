namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

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
            SpellDetector.OnCreateSpell += KarmaQ;
        }

        #endregion

        #region Methods

        private static void KarmaQ(Obj_AI_Base sender, MissileClient missile, SpellData data, SpellArgs spellArgs)
        {
            if (data.MenuName != "KarmaQMantra")
            {
                return;
            }

            var dir = (missile.EndPosition - missile.StartPosition).To2D().Normalized();
            var spell =
                Evade.SpellsDetected.Values.FirstOrDefault(
                    i =>
                    i.Data.MenuName == "KarmaQ" && i.Unit.NetworkId == sender.NetworkId
                    && dir.AngleBetween(i.Direction) < 5 && i.Start.Distance(missile.StartPosition) < 100);

            if (spell != null)
            {
                Evade.SpellsDetected.Remove(spell.SpellId);
            }
        }

        #endregion
    }
}