namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;

    using vEvade.Managers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Ekko : IChampionManager
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
            SpellDetector.OnProcessSpell += EkkoR;
        }

        #endregion

        #region Methods

        private static void EkkoR(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "EkkoR")
            {
                return;
            }

            foreach (var obj in
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(i => i.IsValid() && !i.IsDead && i.Name == "Ekko" && i.Team == sender.Team))
            {
                SpellDetector.AddSpell(sender, sender.ServerPosition, obj.ServerPosition, data);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}