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

    public class Syndra : IChampionManager
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
            SpellDetector.OnProcessSpell += SyndraE;
        }

        #endregion

        #region Methods

        private static void SyndraE(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (!data.MenuName.StartsWith("SyndraE"))
            {
                return;
            }

            SpellDetector.AddSpell(sender, sender.ServerPosition, args.End, data);
            var spell =
                Evade.DetectedSpells.Values.FirstOrDefault(
                    i => i.Data.MenuName == data.MenuName && i.Unit.CompareId(sender));
            SpellData eqData;

            if (spell != null && Evade.OnProcessSpells.TryGetValue("SyndraEQ", out eqData))
            {
                var cone =
                    new Polygons.Cone(
                        spell.Cone.Center,
                        spell.Cone.Direction,
                        spell.Data.RawRadius + 30,
                        spell.Data.RawRange + 200).ToPolygon();
                var orbs =
                    Evade.DetectedSpells.Values.Where(
                        i =>
                        i.Data.MenuName == "SyndraQ" && i.Unit.CompareId(spell.Unit)
                        && Utils.GameTimeTickCount - i.StartTick < 400).Select(i => i.End).ToList();
                orbs.AddRange(
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            i =>
                            i.IsValid() && !i.IsDead && i.CharData.BaseSkinName == "syndrasphere"
                            && i.Team == sender.Team)
                        .Select(i => i.ServerPosition.To2D()));

                foreach (var orb in orbs.Where(i => !cone.IsOutside(i)))
                {
                    var startPos = orb.Extend(spell.Start, 100);
                    var endPos = spell.Start.Extend(orb, spell.Start.Distance(orb) > 200 ? 1300 : 1100);
                    var startT = spell.StartTick + data.Delay
                                 + (int)(spell.Start.Distance(orb) / data.MissileSpeed * 1000);
                    SpellDetector.AddSpell(sender, startPos, endPos, eqData, null, SpellType.None, true, startT);
                }
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}