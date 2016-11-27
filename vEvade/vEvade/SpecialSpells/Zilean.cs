namespace vEvade.SpecialSpells
{
    #region

    using System;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Zilean : IChampionManager
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
            Game.OnUpdate += ZileanQ;
        }

        #endregion

        #region Methods

        private static void ZileanQ(EventArgs args)
        {
            foreach (var spell in
                Evade.DetectedSpells.Values.Where(
                    i =>
                    i.Data.MenuName == "ZileanQ" && i.EndTick <= Utils.GameTimeTickCount && i.MissileObject == null
                    && i.ToggleObject == null))
            {
                Utility.DelayAction.Add(1, () => Evade.DetectedSpells.Remove(spell.SpellId));
            }
        }

        #endregion
    }
}