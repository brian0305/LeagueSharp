using System;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugins
{
    internal class Nidalee : Helper
    {
        public Nidalee()
        {
            Q = new Spell(SpellSlot.Q, 1500, TargetSelector.DamageType.Magical);
            Q.SetSkillshot(0.125f, 40, 1300, true, SkillshotType.SkillshotLine);
            Game.OnUpdate += OnUpdate;
        }

        private void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            if (Orbwalk.CurrentMode == Orbwalker.Mode.Combo && Q.IsReady())
            {
                //Q.CastOnBestTarget();
                var target = Q.GetTarget();
                if (target != null)
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
                }
            }
        }
    }
}