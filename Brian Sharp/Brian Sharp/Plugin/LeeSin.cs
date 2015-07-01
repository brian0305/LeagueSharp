using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class LeeSin : Helper
    {
        private static Vector3 _wardPlacePos;

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 430, TargetSelector.DamageType.Magical);
            E2 = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 375);
            Q.SetSkillshot(0.25f, 65, 1800, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "P", "Use Passive", false);
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QCol", "-> Smite Collision");
                    AddBool(comboMenu, "W", "Use W");
                    AddSlider(comboMenu, "WHpU", "-> If Hp Under", 30);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    champMenu.AddSubMenu(comboMenu);
                }
                //var harassMenu = new Menu("Harass", "Harass");
                //{
                //    AddBool(harassMenu, "Q", "Use Q");
                //    AddSlider(harassMenu, "Q2HpA", "-> Q2 If Hp Above", 30);
                //    AddBool(harassMenu, "E", "Use E");
                //    AddBool(harassMenu, "W", "Use W Jump Back");
                //    AddBool(harassMenu, "WWard", "-> Ward Jump", false);
                //    champMenu.AddSubMenu(harassMenu);
                //}
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "W", "Use W");
                    AddBool(fleeMenu, "PinkWard", "-> Ward Jump Use Pink Ward", false);
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "R", "Use R");
                        AddBool(interruptMenu, "RWard", "-> Ward Jump");
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
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private static bool HaveP
        {
            get { return Player.HasBuff("blindmonkpassive_cosmetic"); }
        }

        private static bool IsQOne
        {
            get { return Q.Instance.SData.Name.ToLower().Contains("one"); }
        }

        private static bool IsWOne
        {
            get { return W.Instance.SData.Name.ToLower().Contains("one"); }
        }

        private static bool IsEOne
        {
            get { return E.Instance.SData.Name.ToLower().Contains("one"); }
        }

        private static IEnumerable<Obj_AI_Base> ObjHaveQ
        {
            get { return ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(Q2.Range) && HaveQ(i)); }
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
                case Orbwalker.Mode.Clear:
                    SmiteMob();
                    break;
                case Orbwalker.Mode.LastHit:
                    LastHit();
                    break;
                case Orbwalker.Mode.Flee:
                    Flee(Game.CursorPos);
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
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position, (IsQOne ? Q : Q2).Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0 && IsWOne)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position, (IsEOne ? E : E2).Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "R") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !R.IsReady())
            {
                return;
            }
            if (R.IsInRange(unit))
            {
                R.CastOnUnit(unit, PacketCast);
            }
            else if (W.IsReady() && IsWOne)
            {
                var pos = unit.ServerPosition.Randomize(0, (int) R.Range);
                if (Flee(pos, false))
                {
                    return;
                }
                if (GetValue<bool>("Interrupt", "RWard"))
                {
                    Flee(pos);
                }
            }
        }

        private static void Fight(string mode)
        {
            if (GetValue<bool>(mode, "P") && HaveP && Orbwalk.GetBestHeroTarget != null && Orbwalk.CanAttack)
            {
                return;
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget();
                    if (target != null)
                    {
                        var pred = Q.GetPrediction(target);
                        if (pred.Hitchance >= HitChance.High && Q.Cast(pred.CastPosition, PacketCast))
                        {
                            return;
                        }
                        if (GetValue<bool>(mode, "QCol") && pred.Hitchance == HitChance.Collision &&
                            pred.CollisionObjects.Count(IsMinion) == 1 && CastSmite(pred.CollisionObjects.First()) &&
                            Q.Cast(pred.CastPosition, PacketCast))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    var target = Q2.GetTarget(0, HeroManager.Enemies.Where(i => !HaveQ(i)));
                    if (target != null &&
                        (QAgain(target) ||
                         ((target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup)) &&
                          Player.Distance(target) > 300 && !R.IsReady()) || Q.IsKillable(target, 1) ||
                         !Orbwalk.InAutoAttackRange(target, 100) || !HaveP) && Q2.Cast(PacketCast))
                    {
                        return;
                    }
                    if (target == null)
                    {
                        var sub = Q2.GetTarget();
                        if (sub != null && ObjHaveQ.Any(i => i.Distance(sub) < Player.Distance(sub)) &&
                            Q2.Cast(PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                if (IsEOne)
                {
                    if (E.GetTarget() != null && E.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (
                    HeroManager.Enemies.Where(i => i.IsValidTarget(E2.Range) && HaveE(i))
                        .Any(i => EAgain(i) || !Orbwalk.InAutoAttackRange(i, 50) || !HaveP) && E2.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "R") && R.IsReady() && GetValue<bool>(mode, "Q") && Q.IsReady() && !IsQOne)
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !HaveQ(i)));
                if (target != null && CanKill(target, GetQ2Dmg(target, R.GetDamage(target))) &&
                    R.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady() && Orbwalk.GetBestHeroTarget != null)
            {
                if (IsWOne)
                {
                    if (!HaveP && !Q.IsReady() && !E.IsReady() && Player.HealthPercent < 50)
                    {
                        W.Cast(PacketCast);
                    }
                }
                else if (!Player.HasBuff("BlindMonkSafeguard") &&
                         (Player.HealthPercent < GetValue<Slider>(mode, "WHpU").Value || !HaveP))
                {
                    W2.Cast(PacketCast);
                }
            }
        }

        private static void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.IsReady() || !IsQOne)
            {
                return;
            }
            var obj =
                MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .Where(
                        i =>
                            Q.GetPrediction(i).Hitchance >= HitChance.High &&
                            (!Orbwalk.InAutoAttackRange(i) || i.Health > Player.GetAutoAttackDamage(i, true)))
                    .FirstOrDefault(i => Q.IsKillable(i));
            if (obj == null)
            {
                return;
            }
            Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
        }

        private static bool Flee(Vector3 pos, bool useWard = true)
        {
            if (!GetValue<bool>("Flee", "W") || !W.IsReady() || !IsWOne)
            {
                return false;
            }
            Obj_AI_Base obj = null;
            var jumpPos = Player.Distance(pos) > W.Range ? Player.ServerPosition.Extend(pos, W.Range) : pos;
            if (_wardPlacePos.IsValid())
            {
                obj =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(
                            i =>
                                i.IsValidTarget(W.Range, false) && i.IsAlly && i.Distance(_wardPlacePos) < 200 &&
                                IsWard(i));
            }
            if (!_wardPlacePos.IsValid() || obj == null)
            {
                obj =
                    HeroManager.Allies.Where(
                        i => !i.IsMe && i.IsValidTarget(W.Range, false) && i.Distance(jumpPos) < 200)
                        .MinOrDefault(i => i.Distance(jumpPos)) ??
                    MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Ally)
                        .Where(i => i.Distance(jumpPos) < 200)
                        .MinOrDefault(i => i.Distance(jumpPos));
            }
            if (obj != null)
            {
                return W.CastOnUnit(obj, PacketCast);
            }
            if (useWard && GetWardSlot != null)
            {
                var subPos = Player.Distance(pos) > GetWardRange
                    ? Player.ServerPosition.Extend(pos, GetWardRange - 30)
                    : pos;
                if (Player.Spellbook.CastSpell(GetWardSlot.SpellSlot, subPos))
                {
                    _wardPlacePos = subPos;
                    Utility.DelayAction.Add(500, () => _wardPlacePos = new Vector3());
                }
            }
            return false;
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
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget();
                    if (target != null && Q.IsKillable(target) &&
                        Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    var target = Q2.GetTarget(0, HeroManager.Enemies.Where(i => !HaveQ(i)));
                    if (target != null && Q.IsKillable(target, 1) && Q2.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady() && IsEOne)
            {
                var target = E.GetTarget();
                if (target != null && E.IsKillable(target) && E.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && R.IsKillable(target))
                {
                    R.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static double GetQ2Dmg(Obj_AI_Base target, double subHp = 0)
        {
            var dmg = new[] { 50, 80, 110, 140, 170 }[Q.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod +
                      0.08 * (target.MaxHealth - (target.Health - subHp));
            return
                Player.CalcDamage(
                    target, Damage.DamageType.Physical, target.IsValid<Obj_AI_Minion>() ? Math.Min(dmg, 400) : dmg) +
                subHp;
        }

        private static bool HaveQ(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkSonicWave");
        }

        private static bool HaveE(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkTempest");
        }

        private static bool QAgain(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time < 0.5f;
        }

        private static bool EAgain(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkTempest");
            return buff != null && buff.EndTime - Game.Time < 0.5f;
        }
    }
}