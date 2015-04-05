using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class JarvanIV : Helper
    {
        private const int RWidth = 325;
        private bool _rCasted;

        public JarvanIV()
        {
            Q = new Spell(SpellSlot.Q, 770);
            W = new Spell(SpellSlot.W, 525);
            E = new Spell(SpellSlot.E, 860, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 650);
            Q.SetSkillshot(0.4f, 70, 1450, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 175, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "QFlagRange", "-> To Flag If Flag In", 500, 100, 880);
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "WHpU", "-> If Player Hp Under", 40);
                    AddItem(comboMenu, "WCountA", "-> If Enemy Above", 2, 1, 5);
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "EQ", "-> Save E For EQ");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Enemy Hp Under", 40);
                    AddItem(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "QHpA", "-> To Flag If Hp Above", 20);
                    AddItem(harassMenu, "QFlagRange", "-> To Flag If Flag In", 500, 100, 880);
                    AddItem(harassMenu, "E", "Use E");
                    AddItem(harassMenu, "EQ", "-> Save E For EQ");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "WHpU", "-> If Hp Under", 40);
                    AddItem(clearMenu, "E", "Use E");
                    AddItem(clearMenu, "Item", "Use Tiamat/Hydra Item");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddItem(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "EQ", "Use EQ");
                    AddItem(fleeMenu, "W", "Use W To Slow Enemy");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "R", "Use R");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddItem(interruptMenu, "EQ", "Use EQ");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddItem(
                                interruptMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "W", "W Range", false);
                    AddItem(drawMenu, "E", "E Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private Obj_AI_Minion Flag
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(i => i.IsAlly && i.Name == "Beacon" && Q.IsInRange(i, 880));
            }
        }

        private void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            if (R.IsReady() && _rCasted && Player.CountEnemiesInRange(RWidth) == 0 && R.Cast(PacketCast))
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

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "EQ") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !Q.IsReady())
            {
                return;
            }
            if (E.CanCast(unit) && Player.Mana >= Q.Instance.ManaCost + E.Instance.ManaCost)
            {
                var predE = E.GetPrediction(unit, true);
                if (predE.Hitchance >= HitChance.High &&
                    E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -E.Width / 2), PacketCast) &&
                    Q.Cast(predE.CastPosition, PacketCast))
                {
                    return;
                }
            }
            if (Flag != null &&
                (unit.Distance(Flag) <= 60 ||
                 (Player.Distance(unit) >= 150 && Q.WillHit(unit, Flag.ServerPosition, 110))))
            {
                Q.Cast(Flag.ServerPosition, PacketCast);
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "JarvanIVCataclysm")
            {
                _rCasted = true;
                Utility.DelayAction.Add(3500, () => _rCasted = false);
            }
        }

        private void Fight(string mode)
        {
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                if (GetValue<bool>(mode, "EQ") &&
                    (Player.Mana < E.Instance.ManaCost + Q.Instance.ManaCost || !Q.IsReady()))
                {
                    return;
                }
                var target = E.GetTarget(E.Width);
                if (target != null)
                {
                    var predE = E.GetPrediction(target, true);
                    if (predE.Hitchance >= HitChance.High &&
                        E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -E.Width / 2), PacketCast))
                    {
                        if (GetValue<bool>(mode, "Q") && Q.IsReady() &&
                            (mode == "Combo" || Player.HealthPercentage() >= GetValue<Slider>(mode, "QHpA").Value))
                        {
                            Q.Cast(predE.CastPosition, PacketCast);
                        }
                        return;
                    }
                }
            }
            if ((!GetValue<bool>(mode, "E") || !E.IsReady()) && GetValue<bool>(mode, "Q") && Q.IsReady() &&
                (mode == "Combo" || (Flag == null || Player.HealthPercentage() >= GetValue<Slider>(mode, "QHpA").Value)))
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    if (GetValue<bool>(mode, "E") && Flag != null &&
                        Player.Distance(Flag) <= GetValue<Slider>(mode, "QFlagRange").Value &&
                        (target.Distance(Flag) <= 60 ||
                         (Player.Distance(target) >= 150 && Q.WillHit(target, Flag.ServerPosition, 110))) &&
                        Q.Cast(Flag.ServerPosition, PacketCast))
                    {
                        return;
                    }
                    if (Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (mode != "Combo")
            {
                return;
            }
            if (GetValue<bool>(mode, "R") && R.IsReady() && !_rCasted)
            {
                var obj =
                    HeroManager.Enemies.Where(
                        i =>
                            i.IsValidTarget(R.Range) &&
                            ((i.CountEnemiesInRange(RWidth) > 1 && R.IsKillable(i)) ||
                             (i.CountEnemiesInRange(RWidth) > 1 &&
                              i.GetEnemiesInRange(RWidth)
                                  .Any(
                                      a =>
                                          a.IsValidTarget() &&
                                          a.HealthPercentage() < GetValue<Slider>(mode, "RHpU").Value)) ||
                             (i.CountEnemiesInRange(RWidth) >= GetValue<Slider>(mode, "RCountA").Value)))
                        .MaxOrDefault(i => i.CountEnemiesInRange(RWidth));

                if (obj != null && R.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady() &&
                Player.CountEnemiesInRange(W.Range) >= GetValue<Slider>(mode, "WCountA").Value &&
                Player.HealthPercentage() < GetValue<Slider>(mode, "WHpU").Value)
            {
                W.Cast(PacketCast);
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() &&
                (minionObj.Count > 1 || minionObj.Any(i => i.MaxHealth >= 1200)))
            {
                var pos = E.GetCircularFarmLocation(minionObj.Where(i => E.IsInRange(i, E.Range + E.Width)).ToList());
                if (pos.MinionsHit > 0 && E.Cast(pos.Position, PacketCast))
                {
                    if (GetValue<bool>("Clear", "Q") && Q.IsReady())
                    {
                        Q.Cast(pos.Position, PacketCast);
                    }
                    return;
                }
            }
            if ((!GetValue<bool>("Clear", "E") || !E.IsReady()) && GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                if (GetValue<bool>("Clear", "E") && Flag != null &&
                    (minionObj.Count(i => i.Distance(Flag) <= 60) > 1 ||
                     minionObj.Count(i => Q.WillHit(i, Flag.ServerPosition, 110)) > 1) &&
                    Q.Cast(Flag.ServerPosition, PacketCast))
                {
                    return;
                }
                var pos = Q.GetLineFarmLocation(minionObj);
                if (pos.MinionsHit > 0 && Q.Cast(pos.Position, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() &&
                Player.HealthPercentage() < GetValue<Slider>("Clear", "WHpU").Value &&
                minionObj.Any(i => W.IsInRange(i)) && W.Cast(PacketCast))
            {
                return;
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

        private void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.IsReady())
            {
                return;
            }
            var obj =
                MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .FirstOrDefault(i => Q.IsKillable(i));
            if (obj == null)
            {
                return;
            }
            Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
        }

        private void Flee()
        {
            if (GetValue<bool>("Flee", "EQ") && Q.IsReady() && E.IsReady() &&
                Player.Mana >= Q.Instance.ManaCost + E.Instance.ManaCost && E.Cast(Game.CursorPos, PacketCast) &&
                Q.Cast(Game.CursorPos, PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Flee", "W") && W.IsReady() && !Q.IsReady() && W.GetTarget() != null)
            {
                W.Cast(PacketCast);
            }
        }

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value || !Q.IsReady())
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
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && R.IsKillable(target))
                {
                    R.CastOnUnit(target, PacketCast);
                }
            }
        }
    }
}