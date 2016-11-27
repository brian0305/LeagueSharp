namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Yorick : IChampionManager
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
            SpellDetector.OnProcessSpell += YorickE;
        }

        #endregion

        #region Methods

        private static void YorickE(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "YorickE")
            {
                return;
            }

            var start = sender.ServerPosition.To2D();
            var end = args.End.To2D();
            var startPos = end.Extend(start, 120);
            var endPos = startPos.Extend(start, -1);
            var startT = Utils.GameTimeTickCount - Game.Ping / 2 + 350 + (int)(start.Distance(startPos) / 1800 * 1000);
            SpellDetector.AddSpell(sender, startPos, endPos, data, null, SpellType.None, true, startT);
            spellArgs.NoProcess = true;
        }

        #endregion
    }
}