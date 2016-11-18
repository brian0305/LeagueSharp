namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Helpers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Lucian : IChampionManager
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
            SpellDetector.OnProcessSpell += LucianQ;
        }

        #endregion

        #region Methods

        private static void LucianQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "LucianQ")
            {
                return;
            }

            var target = args.Target as Obj_AI_Base;

            if (target == null || !target.IsValid || (!Configs.Debug && !target.IsMe))
            {
                return;
            }

            SpellDetector.AddSpell(
                sender,
                sender.ServerPosition,
                target.Position
                + (target.ServerPosition - target.Position).Normalized() * target.MoveSpeed
                * ((data.Delay - Game.Ping) / 1000f),
                data);
            spellArgs.NoProcess = true;
        }

        #endregion
    }
}