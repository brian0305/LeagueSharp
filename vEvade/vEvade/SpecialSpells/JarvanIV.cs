namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.Core;
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

            SpellData spellData;

            if (!Evade.OnProcessSpells.TryGetValue("JarvanIVQE", out spellData))
            {
                return;
            }

            var startPosQ = sender.ServerPosition.To2D();
            var endPosQ = startPosQ.Extend(args.End.To2D(), spellData.Range);
            var endPos = Vector3.Zero;

            foreach (var spell in
                Evade.SpellsDetected.Values.Where(
                    i =>
                    i.Data.MenuName == "JarvanIVE" && i.Unit.NetworkId == sender.NetworkId
                    && i.End.Distance(startPosQ, endPosQ, true) <= 200))
            {
                endPos = spell.End.To3D();
                break;
            }

            if (!endPos.IsValid())
            {
                foreach (var flag in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            i =>
                            i.IsValid() && !i.IsDead && i.IsVisible && i.Name == "Beacon" && i.Team == sender.Team
                            && i.ServerPosition.To2D().Distance(startPosQ, endPosQ, true) <= 200))
                {
                    var buff = flag.GetBuff("JarvanIVDemacianStandard");

                    if (buff == null || buff.Caster.NetworkId != sender.NetworkId)
                    {
                        continue;
                    }

                    endPos = flag.ServerPosition;
                    break;
                }
            }

            if (endPos.IsValid())
            {
                SpellDetector.AddSpell(sender, startPosQ.To3D(), endPos.Extend(startPosQ.To3D(), -110), spellData);
            }
        }

        #endregion
    }
}