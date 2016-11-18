namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;

    using vEvade.Helpers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Fizz : IChampionManager
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
            SpellDetector.OnProcessSpell += FizzQ;
        }

        #endregion

        #region Methods

        private static void FizzQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "FizzQ")
            {
                return;
            }

            var target = args.Target as Obj_AI_Base;

            if (target != null && target.IsValid && (target.IsMe || Configs.Debug))
            {
                SpellDetector.AddSpell(sender, sender.ServerPosition, target.ServerPosition, data);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}