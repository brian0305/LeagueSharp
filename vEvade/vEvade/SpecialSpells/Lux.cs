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
                    i => i.ChampionName == spellData.ChampName && (Configs.Debug || i.IsEnemy));

            if (hero == null)
            {
                return;
            }

            GameObject.OnCreate += (sender, args) => LuxR(sender, hero, spellData);
        }

        #endregion

        #region Methods

        private static void LuxR(GameObject sender, Obj_AI_Hero hero, SpellData spellData)
        {
            if (!sender.Name.Contains("Lux") || !sender.Name.Contains("R_mis_beam_middle"))
            {
                return;
            }

            var hiuDir = HiuManager.GetLastHiuOrientation();

            if (!hiuDir.IsValid())
            {
                return;
            }

            var startPos = sender.Position.To2D() - hiuDir * spellData.Range / 2;
            var endPos = sender.Position.To2D() + hiuDir * spellData.Range / 2;
            var dir = (endPos - startPos).Normalized();

            if (
                !Evade.SpellsDetected.Values.Any(
                    i =>
                    i.Data.MenuName == spellData.MenuName && i.Unit.NetworkId == hero.NetworkId
                    && dir.AngleBetween(i.Direction) < 3 && startPos.Distance(i.Start) < 100)
                && Configs.Menu.Item("DodgeFoW").GetValue<bool>())
            {
                SpellDetector.AddSpell(hero, startPos.To3D(), endPos.To3D(), spellData);
            }
        }

        #endregion
    }
}