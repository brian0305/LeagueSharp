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
            if (data.MenuName != "SyndraE")
            {
                return;
            }

            var newData = (SpellData)data.Clone();
            newData.Radius = args.SData.Name.EndsWith("5") ? data.RadiusEx : data.Radius;
            SpellDetector.AddSpell(sender, sender.ServerPosition, args.End, newData);
            var spellE =
                Evade.SpellsDetected.Values.FirstOrDefault(
                    i =>
                    i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId
                    && sender.Distance(i.Start) < 100);

            if (spellE != null)
            {
                var sector =
                    new Polygons.Cone(spellE.Start, spellE.Direction, spellE.Radius + 30, spellE.Data.Range + 100)
                        .ToPolygon();
                var orbs =
                    Evade.SpellsDetected.Values.Where(
                        i => i.Data.MenuName == "SyndraQ" && i.Unit.NetworkId == spellE.Unit.NetworkId)
                        .Select(i => i.End)
                        .ToList();
                orbs.AddRange(
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            i =>
                            i.IsValid() && !i.IsDead && i.CharData.BaseSkinName == "syndrasphere"
                            && i.Team == sender.Team)
                        .Select(i => i.ServerPosition.To2D()));

                foreach (var orb in orbs.Where(i => !sector.IsOutside(i)))
                {
                    var orbData = (SpellData)data.Clone();
                    orbData.Range = 950;
                    orbData.Delay = 0;
                    orbData.Radius = 55;
                    orbData.MissileSpeed = 2000;
                    orbData.Type = SpellType.Line;
                    SpellDetector.AddSpell(
                        sender,
                        orb.To3D(),
                        spellE.Start.Extend(orb, spellE.Start.Distance(orb) > 200 ? 1300 : 1000).To3D(),
                        orbData,
                        null,
                        SpellType.None,
                        true,
                        spellE.StartTick + data.Delay + (int)(spellE.Start.Distance(orb) / data.MissileSpeed * 1000));
                }
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}