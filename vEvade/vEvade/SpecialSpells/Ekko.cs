namespace vEvade.SpecialSpells
{
    #region

    using System;
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
            SpellDetector.OnCreateSpell += EkkoW;
            SpellDetector.OnProcessSpell += EkkoR;
            GameObject.OnCreate += EkkoW2;
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

        private static void EkkoW(Obj_AI_Base sender, MissileClient missile, SpellData data, SpellArgs spellArgs)
        {
            if (data.MenuName != "EkkoW")
            {
                return;
            }

            var spell =
                Evade.DetectedSpells.Values.FirstOrDefault(
                    i =>
                    i.Data.MenuName == data.MenuName && i.Unit.CompareId(sender)
                    && i.End.Distance(missile.EndPosition) < 100);

            if (spell != null)
            {
                Evade.DetectedSpells.Remove(spell.SpellId);
            }
        }

        private static void EkkoW2(GameObject sender, EventArgs args)
        {
            var obj = sender as Obj_GeneralParticleEmitter;

            if (obj == null || !obj.IsValid || !new Regex("Ekko_.+_W_Detonate.troy").IsMatch(obj.Name))
            {
                return;
            }

            var spell =
                Evade.DetectedSpells.Values.FirstOrDefault(
                    i => i.Data.MenuName == "EkkoW" && i.End.Distance(obj.Position) < 100);

            if (spell != null)
            {
                Evade.DetectedSpells.Remove(spell.SpellId);
            }
        }

        #endregion
    }
}