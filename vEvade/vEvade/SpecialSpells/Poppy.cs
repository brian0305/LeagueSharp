namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;

    using vEvade.Core;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Poppy : IChampionManager
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
            SpellDetector.OnProcessSpell += PoppyR;
            SpellDetector.OnCreateSpell += PoppyRMissile;
        }

        #endregion

        #region Methods

        private static void PoppyR(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "PoppyRCharge")
            {
                return;
            }

            var newData = (SpellData)data.Clone();
            newData.FixedRange = true;
            spellArgs.NewData = newData;
        }

        private static void PoppyRMissile(
            Obj_AI_Base sender,
            MissileClient missile,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "PoppyRCharge")
            {
                return;
            }

            var spell =
                Evade.SpellsDetected.Values.FirstOrDefault(
                    i => i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId);

            if (spell != null)
            {
                Evade.SpellsDetected.Remove(spell.SpellId);
            }

            var newData = (SpellData)data.Clone();
            newData.Delay = 0;
            spellArgs.NewData = newData;
        }

        #endregion
    }
}