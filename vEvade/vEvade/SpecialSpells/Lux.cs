namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
    using vEvade.Helpers;
    using vEvade.Managers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Lux : IChampionManager
    {
        #region Public Methods and Operators

        public void LoadSpecialSpell(SpellData spellData)
        {
            if (spellData.MenuName != "LuxR")
            {
                return;
            }

            var hero =
                HeroManager.AllHeroes.FirstOrDefault(
                    i => i.ChampionName == spellData.ChampName && (i.IsEnemy || Configs.Debug));

            if (hero == null)
            {
                return;
            }

            GameObject.OnCreate += (sender, args) => LuxR(sender, hero, spellData);
        }

        #endregion

        #region Methods

        private static void LuxR(GameObject sender, Obj_AI_Hero hero, SpellData data)
        {
            var obj = sender as Obj_GeneralParticleEmitter;

            if (obj == null || !obj.IsValid || !new Regex("Lux_.+_R_mis_beam_middle").IsMatch(obj.Name))
            {
                return;
            }

            var startT = Utils.GameTimeTickCount;
            var alreadyAdd =
                Evade.SpellsDetected.Values.Any(
                    i => i.Data.MenuName == data.MenuName && i.Unit.NetworkId == hero.NetworkId);

            if (alreadyAdd)
            {
                return;
            }

            var dir = HiuManager.GetLastHiuOrientation(startT);

            if (dir.IsValid())
            {
                SpellDetector.AddSpell(
                    hero,
                    obj.Position.To2D() - dir * (data.Range / 2f),
                    obj.Position.To2D() + dir * (data.Range / 2f),
                    data);
            }
        }

        #endregion
    }
}