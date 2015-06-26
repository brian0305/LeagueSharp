using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using BrianSharp.Evade;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Yasuo : Helper
    {
        private const int QCirWidth = 300, RWidth = 400;

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 500);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 1200);
            Q.SetSkillshot(GetQDelay, 20, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(GetQ2Delay, 90, 1500, false, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "EDmg", "-> Deal Damage (Q Must On)");
                    AddBool(comboMenu, "EGap", "-> Gap Closer");
                    AddSlider(comboMenu, "EGapRange", "-> If Enemy Not In", 300, 1, 475);
                    AddBool(comboMenu, "EGapTower", "-> Under Tower", false);
                    AddBool(comboMenu, "R", "Use R");
                    AddBool(comboMenu, "RDelay", "-> Delay");
                    AddSlider(comboMenu, "RDelayTime", "--> Time (ms)", 200, 200, 400);
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp Under", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle, true);
                    AddBool(harassMenu, "AutoQ3", "-> Use Q3", false);
                    AddBool(harassMenu, "AutoQTower", "-> Under Tower", false);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "Q3", "-> Use Q3");
                    AddBool(harassMenu, "QTower", "-> Under Tower");
                    AddBool(harassMenu, "QLastHit", "-> Last Hit (Q1/Q2)");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "Q3", "-> Use Q3");
                    AddBool(clearMenu, "E", "Use E");
                    AddBool(clearMenu, "ETower", "-> Under Tower", false);
                    AddBool(clearMenu, "Item", "Use Tiamat/Hydra Item");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    AddBool(lastHitMenu, "Q3", "-> Use Q3");
                    AddBool(lastHitMenu, "E", "Use E");
                    AddBool(lastHitMenu, "ETower", "-> Under Tower", false);
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "E", "Use E");
                    AddBool(fleeMenu, "EStackQ", "-> Stack Q While Dashing");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    if (HeroManager.Enemies.Any())
                    {
                        new Evade(miscMenu);
                        new Target(miscMenu);
                    }
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "Q", "Use Q3");
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
                    AddKeybind(miscMenu, "StackQ", "Auto Stack Q", "Z", KeyBindType.Toggle);
                    AddBool(miscMenu, "StackQDraw", "-> Draw Text");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
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

        private float GetQDelay
        {
            get { return 0.4f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f)); }
        }

        private float GetQ2Delay
        {
            get { return 0.5f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f)); }
        }

        private bool HaveQ3
        {
            get { return Player.HasBuff("YasuoQ3W"); }
        }

        private Obj_AI_Hero QCirTarget
        {
            get
            {
                var pos = Player.GetDashInfo().EndPos.To3D();
                var target = TargetSelector.GetTarget(QCirWidth, TargetSelector.DamageType.Physical, true, null, pos);
                return target != null && Player.Distance(target) < QCirWidth && Player.Distance(pos) < 150
                    ? target
                    : null;
            }
        }

        private void OnUpdate(EventArgs args)
        {
            if (!Equals(Q.Delay, GetQDelay))
            {
                Q.Delay = GetQDelay;
                Q2.Delay = GetQ2Delay;
            }
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
                case Orbwalker.Mode.LastHit:
                    LastHit();
                    break;
                case Orbwalker.Mode.Flee:
                    Flee();
                    break;
            }
            AutoQ();
            KillSteal();
            StackQ();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<KeyBind>("Misc", "StackQ").Active && GetValue<bool>("Misc", "StackQDraw"))
            {
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Orange, "Auto Stack Q");
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position, Player.IsDashing() ? QCirWidth : (!HaveQ3 ? Q : Q2).Range,
                    Q.IsReady() ? Color.Green : Color.Red);
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

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !HaveQ3)
            {
                return;
            }
            if (E.IsReady() && Q.IsReady(50))
            {
                if (E.IsInRange(unit) && CanCastE(unit) && unit.Distance(PosAfterE(unit)) < QCirWidth &&
                    E.CastOnUnit(unit, PacketCast))
                {
                    return;
                }
                if (E.IsInRange(unit, E.Range + QCirWidth))
                {
                    var obj = GetNearObj(unit, true);
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (!Q.IsReady())
            {
                return;
            }
            if (Player.IsDashing())
            {
                var pos = Player.GetDashInfo().EndPos;
                if (Player.Distance(pos) < 40 && unit.Distance(pos) < QCirWidth)
                {
                    CastQCir(unit);
                }
            }
            else
            {
                Q2.CastIfHitchanceEquals(unit, HitChance.High, PacketCast);
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo")
            {
                if (GetValue<bool>(mode, "R") && R.IsReady())
                {
                    var obj = (from enemy in HeroManager.Enemies.Where(i => R.IsInRange(i) && CanCastR(i))
                        let sub = enemy.GetEnemiesInRange(RWidth).Where(CanCastR).ToList()
                        where
                            (sub.Count > 1 && R.IsKillable(enemy)) ||
                            sub.Any(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) ||
                            sub.Count >= GetValue<Slider>(mode, "RCountA").Value
                        select enemy).ToList();
                    if (obj.Any())
                    {
                        var target =
                            obj.Where(
                                i =>
                                    !GetValue<bool>(mode, "RDelay") ||
                                    TimeLeftR(i) <= (float) GetValue<Slider>(mode, "RDelayTime").Value / 1000)
                                .MaxOrDefault(i => i.GetEnemiesInRange(RWidth).Count(CanCastR));
                        if (target != null && R.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                }
                if (GetValue<bool>(mode, "E") && E.IsReady())
                {
                    if (GetValue<bool>(mode, "EDmg") && GetValue<bool>(mode, "Q") && Q.IsReady(50))
                    {
                        var target = Q.GetTarget();
                        if (target != null)
                        {
                            var obj = GetNearObj(target, true);
                            if (obj != null && E.CastOnUnit(obj, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                    if (GetValue<bool>(mode, "EGap"))
                    {
                        var target = Q.GetTarget() ?? Q2.GetTarget();
                        if (target != null)
                        {
                            var obj = GetNearObj(target);
                            if (obj != null &&
                                (obj.NetworkId != target.NetworkId
                                    ? Player.Distance(target) > GetValue<Slider>(mode, "EGapRange").Value
                                    : !Orbwalk.InAutoAttackRange(target)) &&
                                (!UnderTower(PosAfterE(obj)) || GetValue<bool>(mode, "EGapTower")) &&
                                E.CastOnUnit(obj, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                if (mode == "Combo" ||
                    ((!HaveQ3 || GetValue<bool>(mode, "Q3")) &&
                     (!UnderTower(Player.ServerPosition) || GetValue<bool>(mode, "QTower"))))
                {
                    if (Player.IsDashing())
                    {
                        if (QCirTarget != null && CastQCir(QCirTarget))
                        {
                            return;
                        }
                    }
                    else
                    {
                        var target = (!HaveQ3 ? Q : Q2).GetTarget();
                        if (target != null)
                        {
                            if (HaveQ3 && mode == "Combo" && GetValue<bool>(mode, "E") && GetValue<bool>(mode, "EDmg") &&
                                E.IsReady() && GetNearObj(target, true) != null)
                            {
                                return;
                            }
                            if ((!HaveQ3 ? Q : Q2).Cast(target, PacketCast, true).IsCasted())
                            {
                                return;
                            }
                        }
                    }
                }
                if (mode == "Harass" && GetValue<bool>(mode, "QLastHit") && Q.GetTarget(100) == null && !HaveQ3 &&
                    !Player.IsDashing())
                {
                    var obj =
                        MinionManager.GetMinions(
                            E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                            .Cast<Obj_AI_Minion>()
                            .FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                    if (obj != null)
                    {
                        Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
                    }
                }
            }
        }

        private void Clear()
        {
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var minionObj =
                    MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Cast<Obj_AI_Minion>()
                        .Where(i => CanCastE(i) && (!UnderTower(PosAfterE(i)) || GetValue<bool>("Clear", "ETower")))
                        .ToList();
                if (minionObj.Any())
                {
                    var obj = minionObj.FirstOrDefault(i => CanKill(i, GetEDmg(i)));
                    if (obj == null && GetValue<bool>("Clear", "Q") && Q.IsReady(50) &&
                        (!HaveQ3 || GetValue<bool>("Clear", "Q3")))
                    {
                        obj = (from i in minionObj
                            let sub =
                                MinionManager.GetMinions(PosAfterE(i), QCirWidth, MinionTypes.All, MinionTeam.NotAlly)
                            where
                                i.Team == GameObjectTeam.Neutral ||
                                (i.Distance(PosAfterE(i)) < QCirWidth && CanKill(i, GetEDmg(i) + GetQDmg(i))) ||
                                sub.Cast<Obj_AI_Minion>().Any(a => CanKill(a, GetQDmg(a))) || sub.Count > 1
                            select i).MaxOrDefault(
                                i =>
                                    MinionManager.GetMinions(
                                        PosAfterE(i), QCirWidth, MinionTypes.All, MinionTeam.NotAlly).Count);
                    }
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady() && (!HaveQ3 || GetValue<bool>("Clear", "Q3")))
            {
                if (Player.IsDashing())
                {
                    var minionObj = MinionManager.GetMinions(
                        Player.GetDashInfo().EndPos.To3D(), QCirWidth, MinionTypes.All, MinionTeam.NotAlly);
                    if (
                        (minionObj.Cast<Obj_AI_Minion>()
                            .Any(i => CanKill(i, GetQDmg(i)) || i.Team == GameObjectTeam.Neutral) || minionObj.Count > 1) &&
                        Player.Distance(Player.GetDashInfo().EndPos) < 50 && CastQCir(minionObj.First()))
                    {
                        return;
                    }
                }
                else
                {
                    var minionObj = MinionManager.GetMinions(
                        (!HaveQ3 ? E : Q2).Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                    if (minionObj.Any())
                    {
                        if (!HaveQ3)
                        {
                            var obj = minionObj.Cast<Obj_AI_Minion>().FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                            if (obj != null && Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast))
                            {
                                return;
                            }
                        }
                        var pos = (!HaveQ3 ? Q : Q2).GetLineFarmLocation(minionObj);
                        if (pos.MinionsHit > 0 && (!HaveQ3 ? Q : Q2).Cast(pos.Position, PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (GetValue<bool>("Clear", "Item") && (Hydra.IsReady() || Tiamat.IsReady()))
            {
                var minionObj = MinionManager.GetMinions(
                    (Hydra.IsReady() ? Hydra : Tiamat).Range, MinionTypes.All, MinionTeam.NotAlly);
                if (minionObj.Count > 2 ||
                    minionObj.Any(
                        i => i.MaxHealth >= 1200 && i.Distance(Player) < (Hydra.IsReady() ? Hydra : Tiamat).Range - 80))
                {
                    if (Tiamat.IsReady())
                    {
                        Tiamat.Cast();
                    }
                    if (Hydra.IsReady())
                    {
                        Hydra.Cast();
                    }
                }
            }
        }

        private void LastHit()
        {
            if (GetValue<bool>("LastHit", "Q") && Q.IsReady() && !Player.IsDashing() &&
                (!HaveQ3 || GetValue<bool>("LastHit", "Q3")))
            {
                var obj =
                    MinionManager.GetMinions(
                        (!HaveQ3 ? E : Q2).Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Cast<Obj_AI_Minion>()
                        .FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                if (obj != null && (!HaveQ3 ? Q : Q2).CastIfHitchanceEquals(obj, HitChance.High, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("LastHit", "E") && E.IsReady())
            {
                var obj =
                    MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Cast<Obj_AI_Minion>()
                        .Where(
                            i =>
                                CanCastE(i) &&
                                (!Orbwalk.InAutoAttackRange(i) || i.Health > Player.GetAutoAttackDamage(i, true)) &&
                                (!UnderTower(PosAfterE(i)) || GetValue<bool>("LastHit", "ETower")))
                        .FirstOrDefault(i => CanKill(i, GetEDmg(i)));
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private void Flee()
        {
            if (!GetValue<bool>("Flee", "E"))
            {
                return;
            }
            if (GetValue<bool>("Flee", "EStackQ") && Q.IsReady() && !HaveQ3 && Player.IsDashing())
            {
                if (QCirTarget != null && CastQCir(QCirTarget))
                {
                    return;
                }
                if (Player.Distance(Player.GetDashInfo().EndPos) < 100)
                {
                    var minionObj = MinionManager.GetMinions(
                        Player.GetDashInfo().EndPos.To3D(), QCirWidth, MinionTypes.All, MinionTeam.NotAlly);
                    if (minionObj.Any() && CastQCir(minionObj.First()))
                    {
                        return;
                    }
                }
            }
            var obj = GetNearObj();
            if (obj == null || !E.IsReady())
            {
                return;
            }
            E.CastOnUnit(obj, PacketCast);
        }

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active || Player.IsDashing() ||
                (HaveQ3 && !GetValue<bool>("Harass", "AutoQ3")) ||
                (UnderTower(Player.ServerPosition) && !GetValue<bool>("Harass", "AutoQTower")))
            {
                return;
            }
            (!HaveQ3 ? Q : Q2).CastOnBestTarget(0, PacketCast, true);
        }

        private void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                if (Player.IsDashing())
                {
                    var target = QCirTarget;
                    if (target != null && CanKill(target, GetQDmg(target)) && CastQCir(target))
                    {
                        return;
                    }
                }
                else
                {
                    var target = (!HaveQ3 ? Q : Q2).GetTarget();
                    if (target != null && CanKill(target, GetQDmg(target)) &&
                        (!HaveQ3 ? Q : Q2).CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastE(i)));
                if (target != null &&
                    (CanKill(target, GetEDmg(target)) ||
                     (GetValue<bool>("KillSteal", "Q") && Q.IsReady(50) &&
                      CanKill(target, GetEDmg(target) + GetQDmg(target)))) && E.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastR(i)));
                if (target != null && R.IsKillable(target))
                {
                    R.CastOnUnit(target, PacketCast);
                }
            }
        }

        private void StackQ()
        {
            if (!GetValue<KeyBind>("Misc", "StackQ").Active || !Q.IsReady() || Player.IsDashing() || HaveQ3)
            {
                return;
            }
            var target = Q.GetTarget();
            if (target != null && !UnderTower(Player.ServerPosition))
            {
                Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
            }
            else
            {
                var minionObj = MinionManager.GetMinions(
                    E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (!minionObj.Any())
                {
                    return;
                }
                var obj = minionObj.Cast<Obj_AI_Minion>().FirstOrDefault(i => CanKill(i, GetQDmg(i))) ??
                          minionObj.MinOrDefault(i => i.Distance(Player));
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
                }
            }
        }

        private bool CastQCir(Obj_AI_Base target)
        {
            return Q.Cast((!HaveQ3 ? Q : Q2).GetPrediction(target).CastPosition, PacketCast);
        }

        private bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        private bool CanCastR(Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Knockback);
        }

        private float TimeLeftR(Obj_AI_Hero target)
        {
            var buff = target.Buffs.FirstOrDefault(i => i.Type == BuffType.Knockup || i.Type == BuffType.Knockback);
            return buff != null ? buff.EndTime - Game.Time : -1;
        }

        public double GetQDmg(Obj_AI_Base target)
        {
            var dmgItem = 0d;
            if (Sheen.IsOwned() && (Sheen.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Trinity.IsOwned() && (Trinity.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            var haveInfinity = ItemData.Infinity_Edge.GetItem().IsOwned();
            var maxCrit = Player.Crit >= 0.85f;
            var dmg = 20 * Q.Level + Player.TotalAttackDamage * (maxCrit ? (haveInfinity ? 1.875 : 1.5) : 1);
            if (!HaveQ3 || Player.IsDashing())
            {
                dmg += dmgItem;
            }
            if (ItemData.Blade_of_the_Ruined_King.GetItem().IsOwned())
            {
                var dmgBotrk = Math.Max(0.08 * target.Health, 10);
                if (target.IsValid<Obj_AI_Minion>())
                {
                    dmgBotrk = Math.Min(dmgBotrk, 60);
                }
                dmg += dmgBotrk;
            }
            return Player.CalcDamage(target, Damage.DamageType.Physical, dmg) +
                   (Player.GetBuffCount("ItemStatikShankCharge") == 100
                       ? Player.CalcDamage(
                           target, Damage.DamageType.Magical, 100 * (maxCrit ? (haveInfinity ? 2.25 : 1.8) : 1))
                       : 0);
        }

        private double GetEDmg(Obj_AI_Base target)
        {
            return Player.CalcDamage(
                target, Damage.DamageType.Magical,
                (50 + 20 * E.Level) * (1 + Math.Max(0, Player.GetBuffCount("YasuoDashScalar") * 0.25)) +
                0.6 * Player.FlatMagicDamageMod);
        }

        private Obj_AI_Base GetNearObj(Obj_AI_Base target = null, bool inQCir = false)
        {
            var pos = target != null ? Prediction.GetPrediction(target, 0.25f).UnitPosition : Game.CursorPos;
            var obj = new List<Obj_AI_Base>();
            obj.AddRange(MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly));
            obj.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(E.Range)));
            return
                obj.Where(i => CanCastE(i) && pos.Distance(PosAfterE(i)) < (inQCir ? QCirWidth : Player.Distance(pos)))
                    .MinOrDefault(i => pos.Distance(PosAfterE(i)));
        }

        private Vector3 PosAfterE(Obj_AI_Base target)
        {
            return Player.ServerPosition.Extend(
                target.ServerPosition, Player.Distance(target) < 410 ? E.Range : Player.Distance(target) + 65);
        }

        private bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + Player.BoundingRadius);
        }

        private class Evade
        {
            public Evade(Menu menu)
            {
                var evadeMenu = new Menu("Evade", "Evade");
                {
					evadeMenu.AddItem(new MenuItem("Credit", "Credit: Evade#"));
                    var evadeSpells = new Menu("Spells", "Spells");
                    {
                        foreach (var spell in EvadeSpellDatabase.Spells)
                        {
                            var sub = new Menu(spell.Name, "ES_" + spell.Name);
                            {
                                AddSlider(sub, "DangerLevel", "Danger Level", spell.DangerLevel, 1, 5);
                                AddBool(sub, "Enabled", "Enabled", false);
                                evadeSpells.AddSubMenu(sub);
                            }
                        }
                        evadeMenu.AddSubMenu(evadeSpells);
                    }
                    foreach (var hero in
                        HeroManager.Enemies.Where(i => SpellDatabase.Spells.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        evadeMenu.AddSubMenu(new Menu("-> " + hero.ChampionName, "Evade_" + hero.ChampionName));
                    }
                    foreach (var spell in
                        SpellDatabase.Spells.Where(i => HeroManager.Enemies.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        var sub = new Menu(spell.SpellName, "SS_" + spell.MenuItemName);
                        {
                            AddSlider(sub, "DangerLevel", "Danger Level", spell.DangerValue, 1, 5);
                            AddBool(sub, "Enabled", "Enabled", !spell.DisabledByDefault);
                            evadeMenu.SubMenu("Evade_" + spell.ChampionName).AddSubMenu(sub);
                        }
                    }
                }
                menu.AddSubMenu(evadeMenu);
                Collisions.Init();
                Game.OnUpdate += OnUpdate;
                SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
                SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
            }

            private static void OnUpdate(EventArgs args)
            {
                SkillshotDetector.DetectedSkillshots.RemoveAll(i => !i.IsActive());
                foreach (var skillshot in SkillshotDetector.DetectedSkillshots)
                {
                    skillshot.OnUpdate();
                }
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                var currentPath = Player.GetWaypoints();
                var safeResult = IsSafe(Player.ServerPosition.To2D());
                var safePath = IsSafePath(currentPath, 100);
                if (!safePath.IsSafe && !safeResult.Safe)
                {
                    TryToEvade(safeResult.SkillshotList, Game.CursorPos.To2D());
                }
            }

            private static void OnDetectSkillshot(Skillshot skillshot)
            {
                var alreadyAdded =
                    SkillshotDetector.DetectedSkillshots.Any(
                        i =>
                            i.SpellData.SpellName == skillshot.SpellData.SpellName &&
                            i.Unit.NetworkId == skillshot.Unit.NetworkId &&
                            skillshot.Direction.AngleBetween(i.Direction) < 5 &&
                            (skillshot.Start.Distance(i.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0));
                if (skillshot.Unit.Team == Player.Team)
                {
                    return;
                }
                if (skillshot.Start.Distance(Player.ServerPosition.To2D()) >
                    (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
                {
                    return;
                }
                if (alreadyAdded && !skillshot.SpellData.DontCheckForDuplicates)
                {
                    return;
                }
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;
                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                            i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range *
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Unit);
                            SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "UFSlash")
                    {
                        skillshot.SpellData.MissileSpeed = 1600 + (int) skillshot.Unit.MoveSpeed;
                    }
                    if (skillshot.SpellData.SpellName == "SionR")
                    {
                        skillshot.SpellData.MissileSpeed = (int) skillshot.Unit.MoveSpeed;
                    }
                    if (skillshot.SpellData.Invert)
                    {
                        var newDirection = -(skillshot.End - skillshot.Start).Normalized();
                        var end = skillshot.Start + newDirection * skillshot.Start.Distance(skillshot.End);
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                            skillshot.Unit);
                        SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }
                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        const int angle = 60;
                        const int subangle = -angle / 2;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.ServerPosition.To2D()).Rotated(
                                subangle * (float) Math.PI / 180);
                        var edge2 = edge1.Rotated(angle * (float) Math.PI / 180);
                        foreach (var skillshotToAdd in from minion in ObjectManager.Get<Obj_AI_Minion>()
                            let v = (minion.ServerPosition - skillshot.Unit.ServerPosition).To2D()
                            where
                                minion.Name == "Seed" && edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                minion.Distance(skillshot.Unit) < 800 && minion.Team != Player.Team
                            let start = minion.ServerPosition.To2D()
                            let end =
                                skillshot.Unit.ServerPosition.Extend(
                                    minion.ServerPosition, skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000).To2D()
                            select
                                new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                                    skillshot.Unit))
                        {
                            SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        var start = skillshot.End - skillshot.Perpendicular * 400;
                        var end = skillshot.End + skillshot.Perpendicular * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;
                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");
                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;
                        bounce1SpellData.Delay =
                            (int) (skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int) (bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);
                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Unit);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Unit);
                        SkillshotDetector.DetectedSkillshots.Add(bounce1);
                        SkillshotDetector.DetectedSkillshots.Add(bounce2);
                    }
                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int) (1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }
                    if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                    {
                        var endPos = new Vector2();
                        foreach (var s in SkillshotDetector.DetectedSkillshots)
                        {
                            if (s.Unit.NetworkId == skillshot.Unit.NetworkId && s.SpellData.Slot == SpellSlot.E)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100, skillshot.Unit);
                                if (!extendedE.IsSafe(s.End))
                                {
                                    endPos = s.End;
                                }
                                break;
                            }
                        }
                        foreach (var m in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            if (m.BaseSkinName == "jarvanivstandard" && m.Team == skillshot.Unit.Team)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100, skillshot.Unit);
                                if (!extendedE.IsSafe(m.Position.To2D()))
                                {
                                    endPos = m.Position.To2D();
                                }
                                break;
                            }
                        }
                        if (endPos.IsValid())
                        {
                            skillshot = new Skillshot(
                                DetectionType.ProcessSpell, SpellDatabase.GetByName("JarvanIVEQ"),
                                Utils.GameTimeTickCount, skillshot.Start, endPos, skillshot.Unit);
                            skillshot.End = endPos + 200 * (endPos - skillshot.Start).Normalized();
                            skillshot.Direction = (skillshot.End - skillshot.Start).Normalized();
                        }
                    }
                }
                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, SpellDatabase.GetByName("OriannaQend"), skillshot.StartTick,
                        skillshot.Start, skillshot.End, skillshot.Unit);
                    SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                }
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }
                SkillshotDetector.DetectedSkillshots.Add(skillshot);
            }

            private static void OnDeleteMissile(Skillshot skillshot, Obj_SpellMissile missile)
            {
                if (skillshot.SpellData.SpellName != "VelkozQ" ||
                    SkillshotDetector.DetectedSkillshots.Count(i => i.SpellData.SpellName == "VelkozQSplit") != 0)
                {
                    return;
                }
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                for (var i = -1; i <= 1; i = i + 2)
                {
                    var skillshotToAdd = new Skillshot(
                        DetectionType.ProcessSpell, spellData, Utils.GameTimeTickCount, missile.Position.To2D(),
                        missile.Position.To2D() + i * skillshot.Perpendicular * spellData.Range, skillshot.Unit);
                    SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                }
            }

            private static List<Obj_AI_Base> GetEvadeTargets(EvadeSpellData spell,
                bool onlyGood = false,
                bool dontCheckForSafety = false)
            {
                var badTargets = new List<Obj_AI_Base>();
                var goodTargets = new List<Obj_AI_Base>();
                var allTargets = new List<Obj_AI_Base>();
                foreach (var targetType in spell.ValidTargets)
                {
                    switch (targetType)
                    {
                        case SpellTargets.AllyChampions:
                            allTargets.AddRange(
                                HeroManager.Allies.Where(i => i.IsValidTarget(spell.MaxRange, false) && !i.IsMe));
                            break;
                        case SpellTargets.AllyMinions:
                            allTargets.AddRange(
                                MinionManager.GetMinions(
                                    Player.Position, spell.MaxRange, MinionTypes.All, MinionTeam.Ally));
                            break;
                        case SpellTargets.AllyWards:
                            allTargets.AddRange(
                                ObjectManager.Get<Obj_AI_Minion>()
                                    .Where(
                                        i =>
                                            IsWard(i) && i.IsValidTarget(spell.MaxRange, false) && i.Team == Player.Team));
                            break;
                        case SpellTargets.EnemyChampions:
                            allTargets.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(spell.MaxRange)));
                            break;
                        case SpellTargets.EnemyMinions:
                            allTargets.AddRange(
                                MinionManager.GetMinions(
                                    Player.Position, spell.MaxRange, MinionTypes.All, MinionTeam.NotAlly));
                            break;
                        case SpellTargets.EnemyWards:
                            allTargets.AddRange(
                                ObjectManager.Get<Obj_AI_Minion>()
                                    .Where(i => IsWard(i) && i.IsValidTarget(spell.MaxRange)));
                            break;
                    }
                }
                foreach (var target in
                    allTargets.Where(i => dontCheckForSafety || IsSafe(i.ServerPosition.To2D()).Safe))
                {
                    if (spell.Slot == SpellSlot.E && target.HasBuff("YasuoDashWrapper"))
                    {
                        continue;
                    }
                    var pathToTarget = new List<Vector2> { Player.ServerPosition.To2D(), target.ServerPosition.To2D() };
                    if (IsSafePath(pathToTarget, Configs.EvadingFirstTimeOffset, spell.Speed, spell.Delay).IsSafe)
                    {
                        goodTargets.Add(target);
                    }
                    if (IsSafePath(pathToTarget, Configs.EvadingSecondTimeOffset, spell.Speed, spell.Delay).IsSafe)
                    {
                        badTargets.Add(target);
                    }
                }
                return goodTargets.Count > 0 ? goodTargets : (onlyGood ? new List<Obj_AI_Base>() : badTargets);
            }

            private static void TryToEvade(IEnumerable<Skillshot> hitBy, Vector2 to)
            {
                var dangerLevel =
                    hitBy.Select(i => GetValue<Slider>("SS_" + i.SpellData.MenuItemName, "DangerLevel").Value)
                        .Concat(new[] { 0 })
                        .Max();
                foreach (var evadeSpell in
                    EvadeSpellDatabase.Spells.Where(i => i.Enabled && i.DangerLevel <= dangerLevel && i.IsReady()))
                {
                    if (evadeSpell.EvadeType == EvadeTypes.Dash && evadeSpell.CastType == CastTypes.Target)
                    {
                        var targets =
                            GetEvadeTargets(evadeSpell)
                                .Where(
                                    i =>
                                        IsSafe(
                                            Player.ServerPosition.Extend(i.ServerPosition, evadeSpell.MaxRange).To2D())
                                            .Safe)
                                .ToList();
                        if (targets.Count > 0)
                        {
                            var closestTarget =
                                targets.OrderBy(
                                    i =>
                                        Player.ServerPosition.Extend(i.ServerPosition, evadeSpell.MaxRange)
                                            .To2D()
                                            .Distance(to)).FirstOrDefault();
                            if (closestTarget != null && Player.Spellbook.CastSpell(evadeSpell.Slot, closestTarget))
                            {
                                return;
                            }
                        }
                    }
                    if (evadeSpell.EvadeType == EvadeTypes.WindWall)
                    {
                        var safeResult = IsSafe(Player.ServerPosition.To2D());
                        if (!safeResult.Safe &&
                            safeResult.SkillshotList.Where(
                                i =>
                                    i.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall) &&
                                    i.IsAboutToHit(evadeSpell.Delay, Player))
                                .Any(
                                    i =>
                                        Player.Spellbook.CastSpell(
                                            evadeSpell.Slot, Player.ServerPosition.Extend(i.MissilePosition.To3D(), 100))))
                        {
                            return;
                        }
                    }
                }
            }

            private static IsSafeResult IsSafe(Vector2 point)
            {
                var result = new IsSafeResult { SkillshotList = new List<Skillshot>() };
                foreach (var skillshot in
                    SkillshotDetector.DetectedSkillshots.Where(i => i.Evade() && !i.IsSafe(point)))
                {
                    result.SkillshotList.Add(skillshot);
                }
                result.Safe = result.SkillshotList.Count == 0;
                return result;
            }

            private static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
            {
                var isSafe = false;
                var intersections = new List<FoundIntersection>();
                var intersection = new FoundIntersection();
                foreach (var sResult in
                    SkillshotDetector.DetectedSkillshots.Where(i => i.Evade())
                        .Select(i => i.IsSafePath(path, timeOffset, speed, delay)))
                {
                    isSafe = sResult.IsSafe;
                    if (sResult.Intersection.Valid)
                    {
                        intersections.Add(sResult.Intersection);
                    }
                }
                if (isSafe)
                {
                    return new SafePathResult(true, intersection);
                }
                var sortedList = intersections.OrderBy(i => i.Distance).ToList();
                return new SafePathResult(false, sortedList.Count > 0 ? sortedList[0] : intersection);
            }

            private struct IsSafeResult
            {
                public bool Safe;
                public List<Skillshot> SkillshotList;
            }
        }

        private class Target
        {
            private static readonly List<SpellData> Spells = new List<SpellData>();
            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            public Target(Menu menu)
            {
                LoadTargetData();
                var targetMenu = new Menu("Wind Wall (Target)", "Target");
                {
                    AddBool(targetMenu, "W", "Use W");
                    AddBool(targetMenu, "BAttack", "-> Basic Attack");
                    AddSlider(targetMenu, "BAttackHpU", "--> If Hp Under", 20);
                    AddBool(targetMenu, "CAttack", "-> Crit Attack");
                    AddSlider(targetMenu, "CAttackHpU", "--> If Hp Under", 40);
                    foreach (var hero in
                        HeroManager.Enemies.Where(i => Spells.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        targetMenu.AddSubMenu(new Menu("-> " + hero.ChampionName, "T_" + hero.ChampionName));
                    }
                    foreach (
                        var spell in Spells.Where(i => HeroManager.Enemies.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        AddBool(
                            targetMenu.SubMenu("T_" + spell.ChampionName), spell.MissileName, spell.DisplayName, false);
                    }
                }
                menu.AddSubMenu(targetMenu);
                Game.OnUpdate += OnUpdate;
                GameObject.OnCreate += ObjSpellMissileOnCreate;
                GameObject.OnDelete += ObjSpellMissileOnDelete;
            }

            private static void OnUpdate(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                if (!W.IsReady() || !GetValue<bool>("Target", "W"))
                {
                    return;
                }
                foreach (var target in
                    DetectedTargets.Where(i => W.IsInRange(i.Obj)))
                {
                    W.Cast(Player.ServerPosition.Extend(target.Obj.Position, 100), PacketCast);
                    return;
                }
            }

            private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
            {
                if (!sender.IsValid<MissileClient>())
                {
                    return;
                }
                var missile = (MissileClient) sender;
                if (!missile.SpellCaster.IsValid<Obj_AI_Hero>() || missile.SpellCaster.Team == Player.Team)
                {
                    return;
                }
                var unit = (Obj_AI_Hero) missile.SpellCaster;
                var spellData =
                    Spells.FirstOrDefault(
                        i =>
                            i.SpellNames.Contains(missile.SData.Name.ToLower()) &&
                            GetItem("T_" + i.ChampionName, i.MissileName) != null &&
                            GetValue<bool>("T_" + i.ChampionName, i.MissileName));
                if (spellData == null && missile.SData.IsAutoAttack() &&
                    (!missile.SData.Name.ToLower().Contains("crit")
                        ? GetValue<bool>("Target", "BAttack") &&
                          Player.HealthPercent < GetValue<Slider>("Target", "BAttackHpU").Value
                        : GetValue<bool>("Target", "CAttack") &&
                          Player.HealthPercent < GetValue<Slider>("Target", "CAttackHpU").Value) && W.IsReady())
                {
                    spellData = new SpellData
                    {
                        ChampionName = unit.ChampionName,
                        SpellNames = new[] { missile.SData.Name }
                    };
                }
                if (spellData == null || !missile.Target.IsMe)
                {
                    return;
                }
                DetectedTargets.Add(new Targets { Obj = missile });
            }

            private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                if (!sender.IsValid<MissileClient>())
                {
                    return;
                }
                var missile = (MissileClient) sender;
                if (missile.SpellCaster.IsValid<Obj_AI_Hero>() && missile.SpellCaster.Team != Player.Team)
                {
                    DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);
                }
            }

            private static void LoadTargetData()
            {
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ahri",
                        SpellNames = new[] { "ahrifoxfiremissiletwo" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ahri",
                        SpellNames = new[] { "ahritumblemissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Akali", SpellNames = new[] { "akalimota" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Annie", SpellNames = new[] { "disintegrate" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Brand",
                        SpellNames = new[] { "brandConflagrationMissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Brand",
                        SpellNames = new[] { "brandWildfire", "brandWildfireMissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Caitlyn",
                        SpellNames = new[] { "caitlynAceintheHoleMissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Cassiopeia",
                        SpellNames = new[] { "cassiopeiatwinfang" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ezreal",
                        SpellNames = new[] { "ezrealarcaneshiftmissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "FiddleSticks",
                        SpellNames = new[] { "fiddlesticksdarkwind", "fiddlesticksdarkwindmissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Gangplank", SpellNames = new[] { "parley" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData { ChampionName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Katarina",
                        SpellNames = new[] { "katarinaq", "katarinaqmis" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Kayle",
                        SpellNames = new[] { "judicatorreckoning" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Leblanc",
                        SpellNames = new[] { "leblancchaosorb", "leblancchaosorbm" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(new SpellData { ChampionName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Malphite",
                        SpellNames = new[] { "seismicshard" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "MissFortune",
                        SpellNames = new[] { "missfortunericochetshot", "missFortunershotextra" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Nami",
                        SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Nunu", SpellNames = new[] { "iceblast" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Pantheon", SpellNames = new[] { "pantheonq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Ryze",
                        SpellNames = new[] { "spellflux", "spellfluxmissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Shen", SpellNames = new[] { "shenvorpalstar" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Swain", SpellNames = new[] { "swaintorment" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrar" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Taric", SpellNames = new[] { "dazzle" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Tristana",
                        SpellNames = new[] { "detonatingshot" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "TwistedFate",
                        SpellNames = new[] { "bluecardattack" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "TwistedFate",
                        SpellNames = new[] { "goldcardattack" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "TwistedFate",
                        SpellNames = new[] { "redcardattack" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Urgot",
                        SpellNames = new[] { "urgotheatseekinghomemissile" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemn" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Veigar",
                        SpellNames = new[] { "veigarprimordialburst" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Viktor",
                        SpellNames = new[] { "viktorpowertransfer" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        ChampionName = "Vladimir",
                        SpellNames = new[] { "vladimirtidesofbloodnuke" },
                        Slot = SpellSlot.E
                    });
            }

            private class SpellData
            {
                public string ChampionName;
                public SpellSlot Slot;
                public string[] SpellNames = { };

                public string MissileName
                {
                    get { return SpellNames.First(); }
                }

                public string DisplayName
                {
                    get { return MissileName + " (" + Slot + ")"; }
                }
            }

            private class Targets
            {
                public MissileClient Obj;
            }
        }
    }
}