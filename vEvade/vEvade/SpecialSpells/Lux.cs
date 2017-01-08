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

            GameObject.OnCreate += HiuManager.OnCreate;
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
                Evade.DetectedSpells.Values.Any(i => i.Data.MenuName == data.MenuName && i.Unit.CompareId(hero));

            if (alreadyAdd)
            {
                return;
            }

            var pos = obj.Position.To2D();
            var dir = HiuManager.GetHiuDirection(startT, pos);

            if (!dir.IsValid())
            {
                return;
            }

            dir *= data.Range / 2f;
            SpellDetector.AddSpell(hero, pos - dir, pos + dir, data, null, SpellType.None, true, startT);
        }

        #endregion
    }
}