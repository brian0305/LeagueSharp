using System;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Udyr : Helper
    {
        private static int _aaCount;
        private static bool _phoenixActive;

        public Udyr()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddSlider(comboMenu, "WHpU", "-> If Hp <", 70);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    champMenu.AddSubMenu(comboMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddSlider(clearMenu, "WHpU", "-> If Hp <", 70);
                    AddBool(clearMenu, "R", "Use R");
                    AddBool(clearMenu, "Item", "Use Tiamat/Hydra Item");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "E", "Use E");
                    AddBool(fleeMenu, "Stack", "-> Passive Stack");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    AddKeybind(miscMenu, "StunCycle", "Stun Cycle", "Z");
                    champMenu.AddSubMenu(miscMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private static Stance CurStance
        {
            get
            {
                if (Player.HasBuff("UdyrTigerStance"))
                {
                    return Stance.Tiger;
                }
                if (Player.HasBuff("UdyrTurtleStance"))
                {
                    return Stance.Turtle;
                }
                if (Player.HasBuff("UdyrBearStance"))
                {
                    return Stance.Bear;
                }
                return Player.HasBuff("UdyrPhoenixStance") ? Stance.Phoenix : Stance.None;
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                if (Player.IsDead)
                {
                    _aaCount = 0;
                }
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case Orbwalker.Mode.Combo:
                    Fight();
                    break;
                case Orbwalker.Mode.Clear:
                    Clear();
                    break;
                case Orbwalker.Mode.Flee:
                    Flee();
                    break;
            }
            if (GetValue<KeyBind>("Misc", "StunCycle").Active)
            {
                StunCycle();
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalk.CurrentMode != Orbwalker.Mode.Clear)
            {
                SmiteMob();
            }
            KillSteal();
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "UdyrTigerStance" || args.SData.Name == "UdyrTurtleStance" ||
                args.SData.Name == "UdyrBearStance" || args.SData.Name == "UdyrPhoenixStance")
            {
                _aaCount = 0;
                if (args.SData.Name != "UdyrPhoenixStance")
                {
                    _phoenixActive = false;
                }
            }
        }

        private static void AfterAttack(AttackableUnit target)
        {
            if ((Orbwalk.CurrentMode != Orbwalker.Mode.Combo && Orbwalk.CurrentMode != Orbwalker.Mode.Clear) ||
                (CurStance != Stance.Tiger && CurStance != Stance.Phoenix))
            {
                return;
            }
            _aaCount += 1;
            if (CurStance == Stance.Phoenix && Player.GetBuffCount("UdyrPhoenixStance") == 1)
            {
                _phoenixActive = true;
                Utility.DelayAction.Add(
                    100, () =>
                    {
                        if (_phoenixActive)
                        {
                            _phoenixActive = false;
                        }
                    });
            }
        }

        private static void Fight()
        {
            var target = E.GetTarget();
            if (target == null)
            {
                return;
            }
            if (GetValue<bool>("Combo", "E") && E.IsReady() && CanCastE(target) && E.Cast(PacketCast))
            {
                return;
            }
            if (Orbwalk.InAutoAttackRange(target, 100) &&
                (!GetValue<bool>("Combo", "E") || E.Level == 0 || !CanCastE(target)))
            {
                if (GetValue<bool>("Combo", "Q") && Q.Cast(PacketCast))
                {
                    return;
                }
                if (GetValue<bool>("Combo", "R") && R.IsReady() &&
                    (!GetValue<bool>("Combo", "Q") || Q.Level == 0 || (CurStance == Stance.Tiger && _aaCount > 1)) &&
                    R.Cast(PacketCast))
                {
                    return;
                }
                if (GetValue<bool>("Combo", "W") && W.IsReady() &&
                    Player.HealthPercent < GetValue<Slider>("Combo", "WHpU").Value &&
                    ((CurStance == Stance.Tiger && _aaCount > 1) ||
                     (CurStance == Stance.Phoenix && (_aaCount > 2 || _phoenixActive)) || (Q.Level == 0 && R.Level == 0)))
                {
                    W.Cast(PacketCast);
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var obj = Orbwalk.GetPossibleTarget;
            if (obj == null)
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "R") && R.IsReady() &&
                (!GetValue<bool>("Clear", "Q") || Q.Level == 0 || (CurStance == Stance.Tiger && _aaCount > 1)) &&
                R.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() &&
                Player.HealthPercent < GetValue<Slider>("Clear", "WHpU").Value &&
                ((CurStance == Stance.Tiger && _aaCount > 1) ||
                 (CurStance == Stance.Phoenix && (_aaCount > 2 || _phoenixActive)) || (Q.Level == 0 && R.Level == 0)) &&
                W.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "Item"))
            {
                var item = Hydra.IsReady() ? Hydra : Tiamat;
                if (item.IsReady())
                {
                    var minionObj = GetMinions(
                        item.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                    if (minionObj.Count > 2 ||
                        minionObj.Any(i => i.MaxHealth >= 1200 && i.Distance(Player) < item.Range - 80))
                    {
                        item.Cast();
                    }
                }
            }
        }

        private static void Flee()
        {
            if (!GetValue<bool>("Flee", "E") || E.Cast(PacketCast) || !GetValue<bool>("Flee", "Stack"))
            {
                return;
            }
            var passive = Player.GetBuffCount("UdyrMonkeyAgilityBuff");
            if (passive == -1 || passive == 3)
            {
                return;
            }
            if (Q.IsReady() &&
                ((Q.Level > W.Level && Q.Level > R.Level) || (Q.Level == W.Level && Q.Level > R.Level) ||
                 (Q.Level == R.Level && Q.Level > W.Level) || (Q.Level == W.Level && Q.Level == R.Level)) &&
                Q.Cast(PacketCast))
            {
                return;
            }
            if (W.IsReady() &&
                ((W.Level > Q.Level && W.Level > R.Level) || (W.Level == Q.Level && W.Level > R.Level) ||
                 (W.Level == R.Level && W.Level > Q.Level) || (W.Level == Q.Level && W.Level == R.Level)) &&
                W.Cast(PacketCast))
            {
                return;
            }
            if (R.IsReady() &&
                ((R.Level > Q.Level && R.Level > W.Level) || (R.Level == Q.Level && R.Level > W.Level) ||
                 (R.Level == W.Level && R.Level > Q.Level) || (R.Level == Q.Level && R.Level == W.Level)))
            {
                R.Cast(PacketCast);
            }
        }

        private static void StunCycle()
        {
            var obj =
                HeroManager.Enemies.Where(i => i.IsValidTarget(E.Range) && CanCastE(i))
                    .MinOrDefault(i => i.Distance(Player));
            if (obj == null)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                return;
            }
            if (E.IsReady() && E.Cast(PacketCast))
            {
                return;
            }
            if (Orbwalk.InAutoAttackRange(obj))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
            }
            else
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, obj.ServerPosition);
            }
        }

        private static void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Smite") &&
                (CurrentSmiteType == SmiteType.Blue || CurrentSmiteType == SmiteType.Red))
            {
                var target = TargetSelector.GetTarget(760, TargetSelector.DamageType.True);
                if (target != null)
                {
                    CastSmite(target);
                }
            }
        }

        private static bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("UdyrBearStunCheck");
        }

        private enum Stance
        {
            Tiger,
            Turtle,
            Bear,
            Phoenix,
            None
        }
    }
}