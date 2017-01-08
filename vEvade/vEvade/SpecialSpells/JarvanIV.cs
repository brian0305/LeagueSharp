namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
    using vEvade.Helpers;
    using vEvade.Managers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class JarvanIV : IChampionManager
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
            SpellDetector.OnProcessSpell += JarvanQ;
        }

        #endregion

        #region Methods

        private static void JarvanQ(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "JarvanIVQ")
            {
                return;
            }

            SpellData qeData;

            if (!Evade.OnProcessSpells.TryGetValue("JarvanIVQE", out qeData))
            {
                return;
            }

            var startPos = sender.ServerPosition.To2D();
            var qeEnd = startPos.Extend(args.End.To2D(), qeData.RawRange);
            var endPos =
                Evade.DetectedSpells.Values.Where(
                    i =>
                    i.Data.MenuName == "JarvanIVE" && i.Unit.CompareId(sender)
                    && i.End.Distance(startPos, qeEnd, true) < qeData.RadiusEx).Select(i => i.End).ToList();
            endPos.AddRange(
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(
                        i =>
                        i.IsValid() && !i.IsDead && i.CharData.BaseSkinName == "jarvanivstandard"
                        && i.Team == sender.Team
                        && i.ServerPosition.To2D().Distance(startPos, qeEnd, true) < qeData.RadiusEx)
                    .Select(i => i.ServerPosition.To2D()));

            if (endPos.Count > 0)
            {
                SpellDetector.AddSpell(sender, startPos, endPos.OrderBy(i => i.Distance(startPos)).First(), qeData);
            }
        }

        #endregion
    }
}