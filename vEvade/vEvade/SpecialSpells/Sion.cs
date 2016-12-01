namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
    using vEvade.Helpers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Sion : IChampionManager
    {
        #region Public Methods and Operators

        public void LoadSpecialSpell(SpellData spellData)
        {
            if (spellData.MenuName != "SionEMinion")
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

            GameObject.OnCreate += (sender, args) => SionE(sender, hero, spellData);
        }

        #endregion

        #region Methods

        private static void SionE(GameObject sender, Obj_AI_Hero hero, SpellData data)
        {
            var obj = sender as Obj_GeneralParticleEmitter;

            if (obj == null || !obj.IsValid || !new Regex("Sion_.+_E_Minion").IsMatch(obj.Name))
            {
                return;
            }

            var spell =
                Evade.DetectedSpells.Values.FirstOrDefault(
                    i => i.Data.MenuName == "SionE" && i.Unit.NetworkId == hero.NetworkId);

            if (spell != null)
            {
                SpellDetector.AddSpell(
                    spell.Unit,
                    obj.Position.To2D(),
                    spell.Start + spell.Direction * data.Range,
                    data);
            }
        }

        #endregion
    }
}