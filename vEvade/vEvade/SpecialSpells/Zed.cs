namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;

    using vEvade.Managers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Zed : IChampionManager
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
            SpellDetector.OnProcessSpell += ZedSpell;
        }

        #endregion

        #region Methods

        private static void ZedSpell(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (!data.MenuName.StartsWith("Zed"))
            {
                return;
            }

            foreach (var shadow in
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(i => i.IsValid() && !i.IsDead && i.IsVisible && i.Name == "Shadow" && i.Team == sender.Team))
            {
                SpellDetector.AddSpell(sender, shadow.ServerPosition, args.End, data);
            }
        }

        #endregion
    }
}