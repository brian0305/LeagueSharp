using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Ryze : Helper
    {
        public Ryze()
        {
            Q = new Spell(SpellSlot.Q, 900, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 600, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 600, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R);
            Q.SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp Under", 70);
                    AddBool(comboMenu, "Seraph", "Use Seraph's Embrace");
                    AddSlider(comboMenu, "SeraphHpU", "-> If Hp Under", 50);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "W", "Use W");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddBool(antiGapMenu, "W", "Use W");
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
                    AddKeybind(miscMenu, "Chase", "Chase", "Z");
                    AddBool(miscMenu, "WTower", "Auto W If Enemy Under Tower");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            AttackableUnit.OnDamage += OnDamage;
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
                case Orbwalker.Mode.LastHit:
                    LastHit();
                    break;
            }
            if (GetValue<KeyBind>("Misc", "Chase").Active)
            {
                Fight("Chase");
            }
            AutoQ();
            KillSteal();
            AutoWUnderTower();
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
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "W") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) ||
                !W.CanCast(gapcloser.Sender))
            {
                return;
            }
            W.CastOnUnit(gapcloser.Sender, PacketCast);
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "W") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !W.CanCast(unit))
            {
                return;
            }
            W.CastOnUnit(unit, PacketCast);
        }

        private void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (args.TargetNetworkId != Player.NetworkId || Orbwalk.CurrentMode != Orbwalker.Mode.Combo)
            {
                return;
            }
            if (GetValue<bool>("Combo", "Seraph") && Seraph.IsReady() &&
                Player.HealthPercent < GetValue<Slider>("Combo", "SeraphHpU").Value)
            {
                Seraph.Cast();
            }
        }

        private void Fight(string mode)
        {
            switch (mode)
            {
                case "Harass":
                    if (GetValue<bool>(mode, "Q") && Q.CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                    if (GetValue<bool>(mode, "W") && W.CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                    if (GetValue<bool>(mode, "E"))
                    {
                        E.CastOnBestTarget(0, PacketCast);
                    }
                    break;
                case "Combo":
                    if (GetValue<bool>(mode, "R") && Q.GetTarget(100) != null &&
                        Q.GetTarget(100).HealthPercent < GetValue<Slider>(mode, "RHpU").Value && R.Cast(PacketCast))
                    {
                        return;
                    }
                    if (Q.CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                    if (W.IsReady() && !E.IsReady() && W.CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                    E.CastOnBestTarget(0, PacketCast);
                    break;
                case "Chase":
                    var target = W.GetTarget();
                    CustomOrbwalk(target);
                    if (target == null)
                    {
                        return;
                    }
                    if (W.CastOnUnit(target, PacketCast) || W.IsReady() || !target.HasBuff("RunePrison"))
                    {
                        return;
                    }
                    if (E.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                    Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
                    break;
            }
        }

        private void Clear()
        {
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var minionObjQ =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Where(i => Q.GetPrediction(i).Hitchance >= HitChance.Medium)
                        .ToList();
                var obj = minionObjQ.Cast<Obj_AI_Minion>().FirstOrDefault(i => Q.IsKillable(i)) ??
                          minionObjQ.MinOrDefault(i => i.Distance(Player));
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast);
                }
            }
            var minionObjW =
                MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .ToList();
            if (!minionObjW.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                var obj = minionObjW.FirstOrDefault(i => W.IsKillable(i));
                if (obj != null && W.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var obj = minionObjW.FirstOrDefault(i => E.IsKillable(i)) ??
                          minionObjW.MaxOrDefault(
                              i =>
                                  MinionManager.GetMinions(i.ServerPosition, 200, MinionTypes.All, MinionTeam.NotAlly)
                                      .Count);
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.IsReady())
            {
                return;
            }
            var obj =
                MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .Where(i => Q.GetPrediction(i).Hitchance >= HitChance.High)
                    .FirstOrDefault(i => Q.IsKillable(i));
            if (obj == null)
            {
                return;
            }
            Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
        }

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value)
            {
                return;
            }
            Q.CastOnBestTarget(0, PacketCast);
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
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) &&
                    Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady())
            {
                var target = W.GetTarget();
                if (target != null && W.IsKillable(target) && W.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null && E.IsKillable(target))
                {
                    E.CastOnUnit(target, PacketCast);
                }
            }
        }

        private void AutoWUnderTower()
        {
            if (!GetValue<bool>("Misc", "WTower") || !W.IsReady())
            {
                return;
            }
            var target = HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range)).MinOrDefault(i => i.Distance(Player));
            var tower =
                ObjectManager.Get<Obj_AI_Turret>()
                    .FirstOrDefault(i => i.IsAlly && !i.IsDead && i.Distance(Player) <= 850);
            if (target != null && tower != null && target.Distance(tower) <= 850)
            {
                W.CastOnUnit(target, PacketCast);
            }
        }
    }
}