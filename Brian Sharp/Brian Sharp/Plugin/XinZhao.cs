using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class XinZhao : Helper
    {
        public XinZhao()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 650, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 500);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp <", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy >=", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    AddBool(clearMenu, "Item", "Use Tiamat/Hydra Item");
                    champMenu.AddSubMenu(clearMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "R", "Use R");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddBool(
                                interruptMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "E", "E Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Orbwalk.OnAttack += OnAttack;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private static List<Obj_AI_Hero> GetRTarget
        {
            get
            {
                return
                    HeroManager.Enemies.Where(
                        i =>
                            i.IsValidTarget() &&
                            Player.Distance(Prediction.GetPrediction(i, 0.25f).UnitPosition) < R.Range).ToList();
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case Orbwalker.Mode.Combo:
                    Fight("Combo");
                    break;
                case Orbwalker.Mode.Harass:
                    Fight("Harass");
                    break;
                case Orbwalker.Mode.Clear:
                    Clear();
                    break;
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalk.CurrentMode != Orbwalker.Mode.Clear)
            {
                SmiteMob();
            }
            KillSteal();
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "R") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !R.IsReady() ||
                unit.HasBuff("xenzhaointimidate"))
            {
                return;
            }
            var pos = Prediction.GetPrediction(unit, 0.25f).UnitPosition;
            if (R.IsInRange(pos) && R.Cast(PacketCast))
            {
                return;
            }
            if (!R.IsInRange(pos) && E.IsReady() && Player.Mana >= E.Instance.ManaCost + R.Instance.ManaCost)
            {
                var obj =
                    (Obj_AI_Base)
                        HeroManager.Enemies.Where(
                            i => i.IsValidTarget(E.Range) && i.Distance(pos) <= R.Range && i.NetworkId != unit.NetworkId)
                            .MinOrDefault(i => i.Distance(unit)) ??
                    GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(i => i.Distance(pos) <= R.Range)
                        .MinOrDefault(i => i.Distance(unit));
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private static void OnAttack(AttackableUnit target)
        {
            if (!W.IsReady())
            {
                return;
            }
            if ((((Orbwalk.CurrentMode == Orbwalker.Mode.Combo || Orbwalk.CurrentMode == Orbwalker.Mode.Harass) &&
                  target is Obj_AI_Hero) || (Orbwalk.CurrentMode == Orbwalker.Mode.Clear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "W"))
            {
                W.Cast(PacketCast);
            }
        }

        private static void AfterAttack(AttackableUnit target)
        {
            if (!Q.IsReady())
            {
                return;
            }
            if ((((Orbwalk.CurrentMode == Orbwalker.Mode.Combo || Orbwalk.CurrentMode == Orbwalker.Mode.Harass) &&
                  target is Obj_AI_Hero) || (Orbwalk.CurrentMode == Orbwalker.Mode.Clear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "Q") && Q.Cast(PacketCast))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady() && !Player.IsDashing())
            {
                var obj = GetRTarget;
                if ((obj.Count > 1 && obj.Any(i => R.IsKillable(i))) ||
                    obj.Any(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) ||
                    obj.Count >= GetValue<Slider>(mode, "RCountA").Value)
                {
                    R.Cast(PacketCast);
                }
                if (GetValue<bool>(mode, "E") && E.IsReady() && Player.Mana >= E.Instance.ManaCost + R.Instance.ManaCost)
                {
                    var target =
                        obj.Where(
                            i => CanKill(i, R.GetDamage(i) + E.GetDamage(i) + Player.GetAutoAttackDamage(i, true)))
                            .MinOrDefault(i => i.Health);
                    if (target != null && R.Cast(PacketCast) && E.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null)
                {
                    if (mode == "Harass")
                    {
                        if (Orbwalk.InAutoAttackRange(target, 100))
                        {
                            E.CastOnUnit(target, PacketCast);
                        }
                    }
                    else if ((!Orbwalk.InAutoAttackRange(target, 20) || Player.Health < target.Health))
                    {
                        E.CastOnUnit(target, PacketCast);
                    }
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var obj = minionObj.FirstOrDefault(i => E.IsKillable(i));
                if (obj == null && !minionObj.Any(i => Orbwalk.InAutoAttackRange(i, 30)))
                {
                    obj = minionObj.MinOrDefault(i => i.Health);
                }
                if (obj != null && E.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Item"))
            {
                var item = Hydra.IsReady() ? Hydra : Tiamat;
                if (item.IsReady() &&
                    (minionObj.Count(i => item.IsInRange(i)) > 2 ||
                     minionObj.Any(i => i.MaxHealth >= 1200 && i.Distance(Player) < item.Range - 80)))
                {
                    item.Cast();
                }
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
                if (target != null && CastSmite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (target != null && E.IsKillable(target) && E.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = GetRTarget.FirstOrDefault(i => R.IsKillable(i));
                if (target != null)
                {
                    R.Cast(PacketCast);
                }
            }
        }
    }
}