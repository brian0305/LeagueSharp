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
    internal class Amumu : Helper
    {
        public Amumu()
        {
            Q = new Spell(SpellSlot.Q, 1100, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 300, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 350, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 550, TargetSelector.DamageType.Magical);
            Q.SetSkillshot(0.25f, 90, 2000, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QCol", "-> Smite Collision");
                    AddBool(comboMenu, "W", "Use W");
                    AddSlider(comboMenu, "WMpA", "-> If Mp Above", 20);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp Under", 60);
                    AddSlider(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddBool(harassMenu, "W", "Use W");
                    AddSlider(harassMenu, "WMpA", "-> If Mp Above", 20);
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddSlider(clearMenu, "WMpA", "-> If Mp Above", 20);
                    AddBool(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
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
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddBool(antiGapMenu, "Q", "Use Q");
                        foreach (var spell in
                            AntiGapcloser.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddBool(
                                antiGapMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(antiGapMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "Q", "Use Q");
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
                    AddSlider(miscMenu, "WExtraRange", "W Extra Range Before Cancel", 60, 0, 200);
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
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private bool HaveW
        {
            get { return Player.HasBuff("AuraofDespair"); }
        }

        private void OnUpdate(EventArgs args)
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

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
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

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "Q") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) || !Q.IsReady())
            {
                return;
            }
            Q.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, PacketCast);
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !Q.CanCast(unit))
            {
                return;
            }
            Q.CastIfHitchanceEquals(unit, HitChance.High, PacketCast);
        }

        private void Fight(string mode)
        {
            if (mode == "Combo")
            {
                if (GetValue<bool>(mode, "R") && R.IsReady() && !Player.IsDashing())
                {
                    var obj = GetRTarget();
                    if (((obj.Count > 1 && obj.Any(i => R.IsKillable(i))) ||
                         (obj.Count > 1 && obj.Any(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value)) ||
                         obj.Count >= GetValue<Slider>(mode, "RCountA").Value) && R.Cast(PacketCast))
                    {
                        return;
                    }
                }
                if (GetValue<bool>(mode, "Q") && Q.IsReady())
                {
                    if (GetValue<bool>(mode, "R") && R.IsReady(100))
                    {
                        var nearObj = new List<Obj_AI_Base>();
                        nearObj.AddRange(MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly));
                        nearObj.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(Q.Range)));
                        var obj = (from i in nearObj
                            where Q.GetPrediction(i).Hitchance >= HitChance.High
                            let enemy = GetRTarget(i.ServerPosition)
                            where
                                (enemy.Count > 1 && enemy.Any(a => R.IsKillable(a))) ||
                                (enemy.Count > 1 &&
                                 enemy.Any(a => a.HealthPercent < GetValue<Slider>(mode, "RHpU").Value)) ||
                                enemy.Count >= GetValue<Slider>(mode, "RCountA").Value
                            select i).MaxOrDefault(i => GetRTarget(i.ServerPosition).Count);
                        if (obj != null && Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast))
                        {
                            return;
                        }
                    }
                    var target = Q.GetTarget();
                    if (target != null && !Orbwalk.InAutoAttackRange(target))
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
            }
            if (GetValue<bool>(mode, "E") && E.IsReady() && E.GetTarget() != null && E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>(mode, "W") && W.IsReady())
            {
                if (Player.ManaPercent >= GetValue<Slider>(mode, "WMpA").Value &&
                    W.GetTarget(GetValue<Slider>("Misc", "WExtraRange").Value) != null)
                {
                    if (!HaveW)
                    {
                        W.Cast(PacketCast);
                    }
                }
                else if (HaveW)
                {
                    W.Cast(PacketCast);
                }
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                if (GetValue<bool>("Clear", "W") && W.IsReady() && HaveW)
                {
                    W.Cast(PacketCast);
                }
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() && minionObj.Any(i => E.IsInRange(i)) && E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                if (Player.ManaPercent >= GetValue<Slider>("Clear", "WMpA").Value &&
                    (minionObj.Count(i => W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value)) > 1 ||
                     minionObj.Any(
                         i =>
                             i.MaxHealth >= 1200 &&
                             W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value))))
                {
                    if (!HaveW && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (HaveW && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var list = minionObj.Where(i => Q.GetPrediction(i).Hitchance >= HitChance.Medium).ToList();
                var obj = list.Cast<Obj_AI_Minion>().FirstOrDefault(i => Q.IsKillable(i)) ??
                          list.FirstOrDefault(i => !Orbwalk.InAutoAttackRange(i));
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast);
                }
            }
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
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) &&
                    Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null && E.IsKillable(target) && E.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = GetRTarget().FirstOrDefault(i => R.IsKillable(i));
                if (target != null)
                {
                    R.Cast(PacketCast);
                }
            }
        }

        private List<Obj_AI_Hero> GetRTarget(Vector3 from = new Vector3())
        {
            var pos = from.IsValid() ? from : Player.ServerPosition;
            return
                HeroManager.Enemies.Where(
                    i => i.IsValidTarget() && Prediction.GetPrediction(i, 0.25f).UnitPosition.Distance(pos) <= R.Range)
                    .ToList();
        }
    }
}