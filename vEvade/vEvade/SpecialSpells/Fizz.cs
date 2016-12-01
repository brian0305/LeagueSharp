namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
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
            SpellDetector.OnCreateSpell += FizzR;
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

        private static void FizzR(Obj_AI_Base sender, MissileClient missile, SpellData data, SpellArgs spellArgs)
        {
            if (data.MenuName != "FizzR")
            {
                return;
            }

            var dist = missile.StartPosition.Distance(missile.EndPosition);
            var radius = dist > 910 ? 440 : (dist >= 455 ? 340 : 0);

            if (radius == 0)
            {
                return;
            }

            var spell =
                Evade.DetectedSpells.Values.FirstOrDefault(
                    i =>
                    i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId
                    && i.Type == SpellType.Circle);

            if (spell != null)
            {
                Evade.DetectedSpells[spell.SpellId].Radius = radius;

                return;
            }

            var newData = (SpellData)data.Clone();
            newData.RadiusEx = radius;
            spellArgs.NewData = newData;
        }

        #endregion
    }
}