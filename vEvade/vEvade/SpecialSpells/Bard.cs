namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;

    using vEvade.Core;
    using vEvade.Helpers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Bard : IChampionManager
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
            SpellDetector.OnCreateSpell += BardR;
        }

        #endregion

        #region Methods

        private static void BardR(Obj_AI_Base sender, MissileClient missile, SpellData data, SpellArgs spellArgs)
        {
            if (data.MenuName != "BardR" || !missile.SData.Name.Contains("Fixed"))
            {
                return;
            }

            var newData = (SpellData)data.Clone();
            newData.MissileSpeed = newData.MissileMinSpeed;
            var spell =
                Evade.DetectedSpells.Values.FirstOrDefault(
                    i => i.Data.MenuName == data.MenuName && i.Unit.CompareId(sender));

            if (spell == null)
            {
                spellArgs.NewData = newData;

                return;
            }

            Evade.DetectedSpells.Remove(spell.SpellId);
            SpellDetector.AddSpell(sender, missile.StartPosition, missile.EndPosition, newData, missile);
        }

        #endregion
    }
}