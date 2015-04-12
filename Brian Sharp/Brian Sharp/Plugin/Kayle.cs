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
    internal class Kayle : Helper
    {
        private readonly Dictionary<int, RAntiItem> _rAntiDetected = new Dictionary<int, RAntiItem>();

        public Kayle()
        {
            Q = new Spell(SpellSlot.Q, 650, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 525, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 900);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    var healMenu = new Menu("Heal (W)", "Heal");
                    {
                        foreach (var name in HeroManager.Allies.Select(MenuName))
                        {
                            AddItem(healMenu, name, name);
                            AddItem(healMenu, name + "HpU", "-> If Hp Under", 40);
                        }
                        comboMenu.AddSubMenu(healMenu);
                    }
                    var saveMenu = new Menu("Save (R)", "Save");
                    {
                        foreach (var name in HeroManager.Allies.Select(MenuName))
                        {
                            AddItem(saveMenu, name, name);
                            AddItem(saveMenu, name + "HpU", "-> If Hp Under", 30);
                        }
                        comboMenu.AddSubMenu(saveMenu);
                    }
                    var antiMenu = new Menu("Anti (R)", "Anti");
                    {
                        AddItem(antiMenu, "Fizz", "Fizz");
                        AddItem(antiMenu, "Karthus", "Karthus");
                        AddItem(antiMenu, "Vlad", "Vladimir");
                        AddItem(antiMenu, "Zed", "Zed");
                        comboMenu.AddSubMenu(antiMenu);
                    }
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "WSpeed", "-> Speed");
                    AddItem(comboMenu, "WHeal", "-> Heal");
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "EAoE", "-> Focus Most AoE Target");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RSave", "-> Save");
                    AddItem(
                        comboMenu, "RAnti", "-> Anti Dangerous Ultimate", new[] { "Off", "Self", "Ally", "Both" }, 3);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddItem(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "Q", "Use Q To Slow Enemy");
                    AddItem(fleeMenu, "W", "Use W");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "W", "W Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private void OnUpdate(EventArgs args)
        {
            AntiDetect();
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
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "E") && GetValue<bool>(mode, "EAoE") &&
                Player.HasBuff("JudicatorRighteousFury"))
            {
                var target =
                    HeroManager.Enemies.Where(i => Orbwalk.InAutoAttackRange(i))
                        .MaxOrDefault(i => i.CountEnemiesInRange(150));
                if (target != null)
                {
                    Orbwalk.ForcedTarget = target;
                }
            }
            else
            {
                Orbwalk.ForcedTarget = null;
            }
            if (GetValue<bool>(mode, "Q"))
            {
                var target = Q.GetTarget();
                if (target != null &&
                    ((Player.Distance(target) > Q.Range - 100 && !target.IsFacing(Player) && Player.IsFacing(target)) ||
                     target.HealthPercentage() > 60) && Q.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady() && E.GetTarget() != null && E.Cast(PacketCast))
            {
                return;
            }
            if (mode != "Combo")
            {
                return;
            }
            if (GetValue<bool>(mode, "W") && W.IsReady())
            {
                if (GetValue<bool>(mode, "WHeal"))
                {
                    var obj =
                        HeroManager.Allies.Where(
                            i =>
                                i.IsValidTarget(W.Range, false) && GetValue<bool>("Heal", MenuName(i)) &&
                                i.HealthPercentage() < GetValue<Slider>("Heal", MenuName(i) + "HpU").Value &&
                                !i.InFountain() && !i.IsRecalling() && i.CountEnemiesInRange(W.Range) > 0 &&
                                !i.HasBuff("JudicatorIntervention") && !i.HasBuff("Undying Rage"))
                            .MinOrDefault(i => i.Health);
                    if (obj != null && W.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
                if (GetValue<bool>(mode, "WSpeed"))
                {
                    var target = Q.GetTarget(200);
                    if (target != null && !target.IsFacing(Player) &&
                        (!Player.HasBuff("JudicatorRighteousFury") ||
                         (Player.HasBuff("JudicatorRighteousFury") && !Orbwalk.InAutoAttackRange(target))) &&
                        (!GetValue<bool>(mode, "Q") ||
                         (GetValue<bool>(mode, "Q") && Q.IsReady() && !Q.IsInRange(target))) && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "R") && R.IsReady())
            {
                if (GetValue<bool>(mode, "RSave"))
                {
                    var obj =
                        HeroManager.Allies.Where(
                            i =>
                                i.IsValidTarget(R.Range, false) && GetValue<bool>("Save", MenuName(i)) &&
                                i.HealthPercentage() < GetValue<Slider>("Save", MenuName(i) + "HpU").Value &&
                                !i.InFountain() && !i.IsRecalling() && i.CountEnemiesInRange(R.Range) > 0 &&
                                !i.HasBuff("Undying Rage")).MinOrDefault(i => i.Health);
                    if (obj != null && R.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
                if (GetValue<StringList>(mode, "RAnti").SelectedIndex > 0)
                {
                    var obj =
                        HeroManager.Allies.Where(
                            i =>
                                i.IsValidTarget(R.Range, false) && _rAntiDetected.ContainsKey(i.NetworkId) &&
                                Game.Time > _rAntiDetected[i.NetworkId].StartTick && !i.HasBuff("Undying Rage"))
                            .MinOrDefault(i => i.Health);
                    if (obj != null)
                    {
                        R.CastOnUnit(obj, PacketCast);
                    }
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
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var obj = minionObj.Cast<Obj_AI_Minion>().FirstOrDefault(i => Q.IsKillable(i)) ??
                          minionObj.FirstOrDefault(i => i.MaxHealth >= 1200);
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() &&
                (minionObj.Count > 1 || minionObj.Any(i => i.MaxHealth >= 1200)))
            {
                E.Cast(PacketCast);
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
            Q.CastOnUnit(obj, PacketCast);
        }

        private void Flee()
        {
            if (GetValue<bool>("Flee", "W") && W.IsReady() && W.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Flee", "Q") && Q.IsReady())
            {
                Q.CastOnBestTarget(0, PacketCast);
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
                if (target != null && Q.IsKillable(target))
                {
                    Q.CastOnUnit(target, PacketCast);
                }
            }
        }

        private void AntiDetect()
        {
            if (Player.IsDead || GetValue<StringList>("Combo", "RAnti").SelectedIndex == 0 || R.Level == 0)
            {
                return;
            }
            var key =
                HeroManager.Allies.FirstOrDefault(
                    i => _rAntiDetected.ContainsKey(i.NetworkId) && Game.Time > _rAntiDetected[i.NetworkId].EndTick);
            if (key != null)
            {
                _rAntiDetected.Remove(key.NetworkId);
            }
            foreach (var obj in
                HeroManager.Allies.Where(i => !i.IsDead && !_rAntiDetected.ContainsKey(i.NetworkId)))
            {
                if ((GetValue<StringList>("Combo", "RAnti").SelectedIndex == 1 && obj.IsMe) ||
                    (GetValue<StringList>("Combo", "RAnti").SelectedIndex == 2 && !obj.IsMe) ||
                    GetValue<StringList>("Combo", "RAnti").SelectedIndex == 3)
                {
                    foreach (var buff in obj.Buffs)
                    {
                        if ((buff.DisplayName == "ZedUltExecute" && GetValue<bool>("Anti", "Zed")) ||
                            (buff.DisplayName == "FizzChurnTheWatersCling" && GetValue<bool>("Anti", "Fizz")) ||
                            (buff.DisplayName == "VladimirHemoplagueDebuff" && GetValue<bool>("Anti", "Vlad")))
                        {
                            _rAntiDetected.Add(obj.NetworkId, new RAntiItem(buff));
                        }
                        else if (buff.DisplayName == "KarthusFallenOne" && GetValue<bool>("Anti", "Karthus") &&
                                 obj.Health <=
                                 ((Obj_AI_Hero) buff.Caster).GetSpellDamage(obj, SpellSlot.R) + obj.Health * 0.2f &&
                                 obj.CountEnemiesInRange(R.Range) > 0)
                        {
                            _rAntiDetected.Add(obj.NetworkId, new RAntiItem(buff));
                        }
                    }
                }
            }
        }

        private string MenuName(Obj_AI_Hero obj)
        {
            return obj.IsMe ? "Self" : obj.ChampionName;
        }

        private class RAntiItem
        {
            public readonly float EndTick;
            public readonly float StartTick;

            public RAntiItem(BuffInstance buff)
            {
                StartTick = Game.Time + (buff.EndTime - buff.StartTime) - (R.Level * 0.5f + 1);
                EndTick = Game.Time + (buff.EndTime - buff.StartTime);
            }
        }
    }
}