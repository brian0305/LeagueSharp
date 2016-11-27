namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;

    using vEvade.Core;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Draven : IChampionManager
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
            SpellDetector.OnProcessSpell += DravenR;
        }

        #endregion

        #region Methods

        private static void DravenR(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "DravenR" || !args.SData.Name.Contains("Double"))
            {
                return;
            }

            var spell =
                Evade.DetectedSpells.Values.FirstOrDefault(
                    i =>
                    i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId && i.MissileObject != null);

            if (spell != null)
            {
                Evade.DetectedSpells.Remove(spell.SpellId);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}