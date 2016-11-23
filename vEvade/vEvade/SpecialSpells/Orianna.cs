namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.Managers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Orianna : IChampionManager
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
            SpellDetector.OnProcessSpell += OriannaSpell;
        }

        #endregion

        #region Methods

        private static void OriannaSpell(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (!data.MenuName.StartsWith("Orianna"))
            {
                return;
            }

            var startPos = Vector3.Zero;

            if (sender.HasBuff("OrianaGhostSelf"))
            {
                startPos = sender.ServerPosition;
            }
            else
            {
                foreach (var hero in
                    HeroManager.AllHeroes.Where(
                        i =>
                        i.IsValid() && !i.IsDead && i.IsVisible && i.Team == sender.Team
                        && i.NetworkId != sender.NetworkId && i.HasBuff("OrianaGhost")))
                {
                    startPos = hero.ServerPosition;
                }

                if (!startPos.IsValid())
                {
                    foreach (var ball in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                i =>
                                i.IsValid() && !i.IsDead && i.IsVisible && i.CharData.BaseSkinName == "oriannaball"
                                && i.Team == sender.Team))
                    {
                        startPos = ball.ServerPosition;
                    }
                }
            }

            if (startPos.IsValid())
            {
                SpellDetector.AddSpell(sender, startPos, args.End, data);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}