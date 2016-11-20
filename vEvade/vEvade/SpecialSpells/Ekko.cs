namespace vEvade.SpecialSpells
{
    #region

    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
    using vEvade.Managers;
    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public class Ekko : IChampionManager
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
            SpellDetector.OnProcessSpell += EkkoR;
            GameObject.OnCreate += EkkoWDetonate;
        }

        #endregion

        #region Methods

        private static void EkkoR(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            if (data.MenuName != "EkkoR")
            {
                return;
            }

            foreach (var obj in
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(i => i.IsValid() && !i.IsDead && i.Name == "Ekko" && i.Team == sender.Team))
            {
                SpellDetector.AddSpell(sender, sender.ServerPosition, obj.ServerPosition, data);
            }

            spellArgs.NoProcess = true;
        }

        private static void EkkoWDetonate(GameObject sender, EventArgs args)
        {
            var toggle = sender as Obj_GeneralParticleEmitter;

            if (toggle == null || !toggle.IsValid || !new Regex("Ekko_.+_W_Detonate").IsMatch(toggle.Name)
                || toggle.Name.Contains("Slow"))
            {
                return;
            }

            var spell =
                Evade.SpellsDetected.Values.FirstOrDefault(
                    i => i.Data.MenuName == "EkkoW" && i.End.Distance(toggle.Position) < 100);

            if (spell != null)
            {
                Evade.SpellsDetected.Remove(spell.SpellId);
            }
        }

        #endregion
    }
}