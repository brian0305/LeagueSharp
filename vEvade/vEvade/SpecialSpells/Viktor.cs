namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;

    using vEvade.Core;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Viktor : IChampionManager
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
            SpellDetector.OnCreateSpell += ViktorE;
        }

        #endregion

        #region Methods

        private static void ViktorE(Obj_AI_Base sender, MissileClient missile, SpellData data, SpellArgs spellArgs)
        {
            if (data.MenuName != "ViktorE" || missile.SData.Name == data.MissileName)
            {
                return;
            }

            SpellData spell;

            if (Evade.OnProcessSpells.TryGetValue("ViktorEExplosion", out spell))
            {
                SpellDetector.AddSpell(sender, missile.StartPosition, missile.EndPosition, spell);
            }
        }

        #endregion
    }
}