using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Tryndamere : Helper
    {
        public Tryndamere()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 830);
            E = new Spell(SpellSlot.E, 660);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0, 93, 1300, false, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddSlider(comboMenu, "QHpU", "-> If Hp <", 40);
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "WSolo", "-> Both Facing", false);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddBool(harassMenu, "E", "Use E");
                    AddSlider(harassMenu, "EHpA", "-> If Hp >=", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "E", "Use E");
                    AddBool(clearMenu, "Item", "Use Tiamat/Hydra");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "E", "Use E");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AttackableUnit.OnDamage += OnDamage;
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
                case Orbwalker.Mode.Flee:
                    if (GetValue<bool>("Flee", "E") && E.IsReady() && E.Cast(Game.CursorPos, PacketCast))
                    {
                        return;
                    }
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
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (args.TargetNetworkId != Player.NetworkId || Orbwalk.CurrentMode != Orbwalker.Mode.Combo)
            {
                return;
            }
            if (GetValue<bool>("Combo", "R") && R.IsReady() && Player.HealthPercent < 10 && R.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Combo", "Q") && Q.IsReady() && !Player.HasBuff("UndyingRage") &&
                Player.HealthPercent < GetValue<Slider>("Survive", "QHpU").Value)
            {
                Q.Cast(PacketCast);
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "W") && W.IsReady() && !Player.IsDashing())
            {
                var target = W.GetTarget();
                if (target != null)
                {
                    if (GetValue<bool>(mode, "WSolo") && Utility.IsBothFacing(Player, target) &&
                        Orbwalk.InAutoAttackRange(target) &&
                        Player.GetAutoAttackDamage(target, true) < target.GetAutoAttackDamage(Player, true))
                    {
                        return;
                    }
                    if (Player.IsFacing(target) && !target.IsFacing(Player) && !Orbwalk.InAutoAttackRange(target, 30) &&
                        W.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady() &&
                (mode == "Combo" || Player.HealthPercent >= GetValue<Slider>(mode, "EHpA").Value))
            {
                var target = E.GetTarget(E.Width);
                if (target != null)
                {
                    var predE = E.GetPrediction(target, true);
                    if (predE.Hitchance >= E.MinHitChance &&
                        ((mode == "Combo" && !Orbwalk.InAutoAttackRange(target, 20)) ||
                         (mode == "Harass" && Orbwalk.InAutoAttackRange(target, 50))))
                    {
                        E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -100), PacketCast);
                    }
                }
                else
                {
                    var sub = E.GetTarget(Orbwalk.GetAutoAttackRange());
                    if (sub != null &&
                        Orbwalk.InAutoAttackRange(sub, 20, Player.ServerPosition.Extend(sub.ServerPosition, E.Range)))
                    {
                        E.Cast(Player.ServerPosition.Extend(sub.ServerPosition, E.Range), PacketCast);
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
                var pos = E.GetLineFarmLocation(minionObj.Cast<Obj_AI_Base>().ToList(), 200);
                if (pos.MinionsHit > 0 && E.Cast(pos.Position.Extend(Player.ServerPosition.To2D(), -100), PacketCast))
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
                var target = E.GetTarget(E.Width);
                if (target != null && E.IsKillable(target))
                {
                    var predE = E.GetPrediction(target, true);
                    if (predE.Hitchance >= E.MinHitChance)
                    {
                        E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -100), PacketCast);
                    }
                }
            }
        }
    }
}