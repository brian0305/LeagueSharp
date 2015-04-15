using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class DrMundo : Helper
    {
        public DrMundo()
        {
            Q = new Spell(SpellSlot.Q, 1050, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 325);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            Q.SetSkillshot(0.25f, 60, 2000, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "QCol", "-> Smite Collision");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "WHpA", "-> If Hp Above", 20);
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Hp Under", 50);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQHpA", "-> If Hp Above", 30);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "W", "Use W");
                    AddItem(harassMenu, "WHpA", "-> If Hp Above", 20);
                    AddItem(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "WHpA", "-> If Hp Above", 20);
                    AddItem(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddItem(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
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
                    AddItem(miscMenu, "WExtraRange", "W Extra Range Before Cancel", 60, 0, 200);
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "W", "W Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Orbwalk.OnAttack += OnAttack;
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
        }

        private void OnAttack(AttackableUnit target)
        {
            if (!E.IsReady())
            {
                return;
            }
            if ((((Orbwalk.CurrentMode == Orbwalker.Mode.Combo || Orbwalk.CurrentMode == Orbwalker.Mode.Harass) &&
                  target is Obj_AI_Hero) || (Orbwalk.CurrentMode == Orbwalker.Mode.Clear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "E"))
            {
                E.Cast(PacketCast);
            }
        }

        private void Fight(string mode)
        {
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                var state = Q.CastOnBestTarget(0, PacketCast);
                if (state.IsCasted())
                {
                    return;
                }
                if (mode == "Combo" && state == Spell.CastStates.Collision && GetValue<bool>(mode, "QCol"))
                {
                    var pred = Q.GetPrediction(Q.GetTarget());
                    if (pred.CollisionObjects.Count(i => i.IsMinion) == 1 && CastSmite(pred.CollisionObjects.First()) &&
                        Q.Cast(pred.CastPosition, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady())
            {
                if (Player.HealthPercentage() >= GetValue<Slider>(mode, "WHpA").Value)
                {
                    if (W.GetTarget(GetValue<Slider>("Misc", "WExtraRange").Value) != null)
                    {
                        if (!Player.HasBuff("BurningAgony") && W.Cast(PacketCast))
                        {
                            return;
                        }
                    }
                    else if (Player.HasBuff("BurningAgony") && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (Player.HasBuff("BurningAgony") && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "R") &&
                Player.HealthPercentage() < GetValue<Slider>(mode, "RHpU").Value && !Player.InFountain() &&
                Q.GetTarget() != null)
            {
                R.Cast(PacketCast);
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                if (GetValue<bool>("Clear", "W") && W.IsReady() && Player.HasBuff("BurningAgony"))
                {
                    W.Cast(PacketCast);
                }
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                if (Player.HealthPercentage() >= GetValue<Slider>("Clear", "WHpA").Value)
                {
                    if (minionObj.Count(i => W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value)) >
                        1 ||
                        minionObj.Any(
                            i =>
                                i.MaxHealth >= 1200 &&
                                W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value)))
                    {
                        if (!Player.HasBuff("BurningAgony") && W.Cast(PacketCast))
                        {
                            return;
                        }
                    }
                    else if (Player.HasBuff("BurningAgony") && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (Player.HasBuff("BurningAgony") && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var obj = minionObj.Cast<Obj_AI_Minion>().FirstOrDefault(i => Q.IsKillable(i)) ??
                          minionObj.FirstOrDefault(i => i.MaxHealth >= 1200);
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast);
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

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.HealthPercentage() < GetValue<Slider>("Harass", "AutoQHpA").Value || !Q.IsReady())
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
                    Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
                }
            }
        }
    }
}