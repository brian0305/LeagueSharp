namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;

    using vEvade.Managers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Lulu : IChampionManager
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
            SpellDetector.OnProcessSpell += LuluQ;
        }

        #endregion

        #region Methods

        private static void LuluQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "LuluQ")
            {
                return;
            }

            foreach (var pix in
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(
                        i =>
                        i.IsValid() && !i.IsDead && i.IsVisible && i.CharData.BaseSkinName == "lulufaerie"
                        && i.Team == sender.Team))
            {
                SpellDetector.AddSpell(sender, pix.ServerPosition, args.End, data);
            }
        }

        #endregion
    }
}