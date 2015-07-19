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
    internal class Kennen : Helper
    {
        public Kennen()
        {
            Q = new Spell(SpellSlot.Q, 1050, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 900, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 500, TargetSelector.DamageType.Magical);
            Q.SetSkillshot(0.125f, 50, 1700, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp <", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy >=", 2, 1, 5);
                    AddBool(comboMenu, "RItem", "-> Use Zhonya When R Active");
                    AddSlider(comboMenu, "RItemHpU", "--> If Hp <", 60);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp >=", 50);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddSlider(harassMenu, "WMpA", "-> If Mp >=", 50);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddSlider(clearMenu, "WHitA", "-> If Hit >=", 2, 1, 5);
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "E", "Use E");
                    AddBool(fleeMenu, "W", "Use W To Stun Enemy");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "W", "Use W");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "W", "Use W");
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
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private static bool HaveE
        {
            get { return Player.HasBuff("KennenLightningRush"); }
        }

        private static bool HaveR
        {
            get { return Player.HasBuff("KennenShurikenStorm"); }
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
            if (Orbwalk.CurrentMode != Orbwalker.Mode.None)
            {
                Orbwalk.Attack = !HaveE;
            }
            else
            {
                Orbwalk.Attack = true;
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
        }

        private static void OnDraw(EventArgs args)
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
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "W") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !W.CanCast(unit) ||
                !HaveW(unit, true))
            {
                return;
            }
            W.Cast(PacketCast);
        }

        private static void Fight(string mode)
        {
            if (GetValue<bool>(mode, "Q") && Q.CastOnBestTarget(0, PacketCast).IsCasted())
            {
                return;
            }
            if (GetValue<bool>(mode, "W") && W.IsReady() &&
                HeroManager.Enemies.Any(i => i.IsValidTarget(W.Range) && HaveW(i)) &&
                (mode == "Combo" || Player.ManaPercent >= GetValue<Slider>(mode, "WMpA").Value))
            {
                if (HaveR)
                {
                    var obj = HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range) && HaveW(i)).ToList();
                    if ((obj.Count(i => HaveW(i, true)) > 1 || obj.Any(i => W.IsKillable(i, 1)) || obj.Count > 2 ||
                         (obj.Count(i => HaveW(i, true)) == 1 && obj.Any(i => !HaveW(i, true)))) && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "R"))
            {
                if (R.IsReady())
                {
                    var obj = GetRTarget;
                    if ((obj.Count > 1 && obj.Any(i => CanKill(i, GetRDmg(i)))) ||
                        obj.Any(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) ||
                        obj.Count >= GetValue<Slider>(mode, "RCountA").Value)
                    {
                        R.Cast(PacketCast);
                    }
                }
                else if (HaveR && GetValue<bool>(mode, "RItem") &&
                         Player.HealthPercent < GetValue<Slider>(mode, "RItemHpU").Value && GetRTarget.Count > 0 &&
                         Zhonya.IsReady())
                {
                    Zhonya.Cast();
                }
            }
        }

        private static void Clear()
        {
            var minionObj = GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var list = minionObj.Where(i => Q.GetPrediction(i).Hitchance >= Q.MinHitChance).ToList();
                var obj = list.FirstOrDefault(i => Q.IsKillable(i)) ?? list.MaxOrDefault(i => i.Distance(Player));
                if (obj != null && Q.Cast(obj, PacketCast).IsCasted())
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() &&
                minionObj.Count(i => W.IsInRange(i) && HaveW(i)) >= GetValue<Slider>("Clear", "WHitA").Value)
            {
                W.Cast(PacketCast);
            }
        }

        private static void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.IsReady())
            {
                return;
            }
            var obj =
                GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Where(i => Q.IsKillable(i))
                    .FirstOrDefault(i => Q.GetPrediction(i).Hitchance >= Q.MinHitChance);
            if (obj == null)
            {
                return;
            }
            Q.Cast(obj, PacketCast);
        }

        private static void Flee()
        {
            if (GetValue<bool>("Flee", "E") && E.IsReady() && !HaveE && E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Flee", "W") && W.IsReady() &&
                HeroManager.Enemies.Any(i => i.IsValidTarget(W.Range) && HaveW(i, true)))
            {
                W.Cast(PacketCast);
            }
        }

        private static void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value)
            {
                return;
            }
            Q.CastOnBestTarget(0, PacketCast);
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
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) && Q.Cast(target, PacketCast).IsCasted())
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady())
            {
                var target = W.GetTarget(0, HeroManager.Enemies.Where(i => !HaveW(i)));
                if (target != null && W.IsKillable(target, 1) && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = GetRTarget.FirstOrDefault(i => CanKill(i, GetRDmg(i)));
                if (target != null)
                {
                    R.Cast(PacketCast);
                }
            }
        }

        private static double GetRDmg(Obj_AI_Hero target)
        {
            return Player.CalcDamage(
                target, Damage.DamageType.Magical,
                (new[] { 80, 145, 210 }[R.Level - 1] + 0.4 * Player.FlatMagicDamageMod) * 3);
        }

        private static bool HaveW(Obj_AI_Base target, bool onlyStun = false)
        {
            return target.HasBuff("KennenMarkOfStorm") && (!onlyStun || target.GetBuffCount("KennenMarkOfStorm") == 2);
        }
    }
}