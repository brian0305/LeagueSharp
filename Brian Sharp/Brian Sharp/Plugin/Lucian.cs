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
    internal class Lucian : Helper
    {
        private bool _qCasted, _wCasted, _eCasted;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 1000, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 425);
            R = new Spell(SpellSlot.R, 1400);
            Q2.SetSkillshot(0.5f, 65, float.MaxValue, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 25, 1600, true, SkillshotType.SkillshotLine); //Width: 55
            R.SetSkillshot(0.5f, 110, 2800, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "P", "Use Passive");
                    AddBool(comboMenu, "PSave", "-> Always Save", false);
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QExtend", "-> Extend");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "WPred", "-> Prediction", false);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "EGap", "-> Gap Closer");
                    AddSlider(comboMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
                    AddList(comboMenu, "EMode", "-> Mode", new[] { "Safe", "Mouse", "Chase" });
                    AddKeybind(comboMenu, "EModeKey", "--> Key Switch", "Z", KeyBindType.Toggle).ValueChanged +=
                        ComboEModeChanged;
                    AddBool(comboMenu, "EModeDraw", "--> Draw Text", false);
                    AddBool(comboMenu, "R", "Use R If Killable");
                    AddBool(comboMenu, "RItem", "-> Use Youmuu For More Damage");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q (Only Extend)", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddBool(harassMenu, "P", "Use Passive");
                    AddBool(harassMenu, "PSave", "-> Always Save", false);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddBool(harassMenu, "E", "Use E");
                    AddSlider(harassMenu, "EHpA", "-> If Hp Above", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    AddSlider(clearMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
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
                        AddBool(killStealMenu, "RStop", "Stop R For Kill Steal");
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "W", "Use W");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    AddBool(miscMenu, "LockR", "Lock R On Target");
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
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private bool HavePassive
        {
            get
            {
                return (Orbwalk.CurrentMode == Orbwalker.Mode.Clear ||
                        GetValue<bool>(Orbwalk.CurrentMode.ToString(), "P")) &&
                       (_qCasted || _wCasted || _eCasted || Player.HasBuff("LucianPassiveBuff"));
            }
        }

        private void ComboEModeChanged(object sender, OnValueChangeEventArgs e)
        {
            var mode = GetValue<StringList>("Combo", "EMode").SelectedIndex;
            GetItem("Combo", "EMode")
                .SetValue(new StringList(GetValue<StringList>("Combo", "EMode").SList, mode == 2 ? 0 : mode + 1));
        }

        private void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            if (Player.IsCastingInterruptableSpell(true))
            {
                if (GetValue<bool>("Misc", "LockR"))
                {
                    LockROnTarget();
                }
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
            AutoQ();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Combo", "E") && GetValue<bool>("Combo", "EModeDraw"))
            {
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Orange, GetValue<StringList>("Combo", "EMode").SelectedValue);
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

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "LucianQ")
            {
                _qCasted = true;
                Utility.DelayAction.Add((int) (Q2.Delay * 1000) + 50, () => _qCasted = false);
            }
            if (args.SData.Name == "LucianW")
            {
                _wCasted = true;
                Utility.DelayAction.Add((int) (W.Delay * 1000) + 50, () => _wCasted = false);
            }
            if (args.SData.Name == "LucianE")
            {
                _eCasted = true;
                Utility.DelayAction.Add(100, () => _eCasted = false);
            }
        }

        private void AfterAttack(AttackableUnit target)
        {
            if (!E.IsReady())
            {
                return;
            }
            if (((Orbwalk.CurrentMode == Orbwalker.Mode.Clear && target is Obj_AI_Minion) ||
                 ((Orbwalk.CurrentMode == Orbwalker.Mode.Combo ||
                   (Orbwalk.CurrentMode == Orbwalker.Mode.Harass &&
                    Player.HealthPercent >= GetValue<Slider>("Harass", "EHpA").Value)) && target is Obj_AI_Hero)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "E") && !HavePassive)
            {
                var obj = (Obj_AI_Base) target;
                if (Orbwalk.CurrentMode == Orbwalker.Mode.Clear || Orbwalk.CurrentMode == Orbwalker.Mode.Harass ||
                    (Orbwalk.CurrentMode == Orbwalker.Mode.Combo &&
                     GetValue<StringList>("Combo", "EMode").SelectedIndex == 0))
                {
                    var pos = Geometry.CircleCircleIntersection(
                        Player.ServerPosition.To2D(), Prediction.GetPrediction(obj, 0.25f).UnitPosition.To2D(), E.Range,
                        Orbwalk.GetAutoAttackRange(obj));
                    if (pos.Count() > 0)
                    {
                        E.Cast(pos.MinOrDefault(i => i.Distance(Game.CursorPos)), PacketCast);
                    }
                    else
                    {
                        E.Cast(Player.ServerPosition.Extend(obj.ServerPosition, -E.Range), PacketCast);
                    }
                }
                else if (Orbwalk.CurrentMode == Orbwalker.Mode.Combo)
                {
                    switch (GetValue<StringList>("Combo", "EMode").SelectedIndex)
                    {
                        case 1:
                            E.Cast(Player.ServerPosition.Extend(Game.CursorPos, E.Range), PacketCast);
                            break;
                        case 2:
                            E.Cast(obj.ServerPosition, PacketCast);
                            break;
                    }
                }
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && CanKill(target, GetRDmg(target)))
                {
                    if (Player.Distance(target) > 550 ||
                        (!Orbwalk.InAutoAttackRange(target) && (!GetValue<bool>(mode, "Q") || !Q.IsReady()) &&
                         (!GetValue<bool>(mode, "W") || !W.IsReady()) && (!GetValue<bool>(mode, "E") || !E.IsReady())))
                    {
                        if (R.CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                        {
                            if (GetValue<bool>(mode, "RItem") && Youmuu.IsReady())
                            {
                                Utility.DelayAction.Add(10, () => Youmuu.Cast());
                            }
                            return;
                        }
                    }
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "E") && GetValue<bool>(mode, "EGap") && E.IsReady())
            {
                var target = E.GetTarget(Orbwalk.GetAutoAttackRange());
                if (target != null && !Orbwalk.InAutoAttackRange(target) &&
                    Orbwalk.InAutoAttackRange(target, 20, Player.ServerPosition.Extend(Game.CursorPos, E.Range)) &&
                    E.Cast(Player.ServerPosition.Extend(Game.CursorPos, E.Range), PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "PSave") && HavePassive)
            {
                return;
            }
            if (GetValue<bool>(mode, "E") &&
                (E.IsReady() || (mode == "Combo" && E.IsReady(GetValue<Slider>(mode, "EDelay").Value))))
            {
                return;
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget();
                if (target != null)
                {
                    if (((Orbwalk.InAutoAttackRange(target) && !HavePassive) ||
                         (!Orbwalk.InAutoAttackRange(target, 20) && Q.IsInRange(target))) &&
                        Q.CastOnUnit(target, PacketCast))
                    {
                        Utility.DelayAction.Add(300, () => Player.IssueOrder(GameObjectOrder.AttackUnit, target));
                        return;
                    }
                    if ((mode == "Harass" || GetValue<bool>(mode, "QExtend")) && !Q.IsInRange(target) &&
                        CastExtendQ(target))
                    {
                        return;
                    }
                }
            }
            if ((!GetValue<bool>(mode, "Q") || !Q.IsReady()) && GetValue<bool>(mode, "W") && W.IsReady() &&
                !Player.IsDashing())
            {
                var target = W.GetTarget();
                if (target != null &&
                    ((Orbwalk.InAutoAttackRange(target) && !HavePassive) || !Orbwalk.InAutoAttackRange(target, 20)))
                {
                    if (mode == "Harass" || GetValue<bool>(mode, "WPred"))
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
                    }
                    else
                    {
                        W.Cast(W.GetPrediction(target).CastPosition, PacketCast);
                    }
                }
            }
        }

        private void Clear()
        {
            var minionObj = MinionManager.GetMinions(
                Q2.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady(GetValue<Slider>("Clear", "EDelay").Value))
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady() && !HavePassive)
            {
                var obj =
                    minionObj.Where(i => Q.IsInRange(i))
                        .MaxOrDefault(
                            i => Q2.CountHits(minionObj, i.ServerPosition.Extend(Player.ServerPosition, -Q2.Range)));
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
                {
                    Utility.DelayAction.Add(300, () => Player.IssueOrder(GameObjectOrder.AttackUnit, obj));
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() && !Player.IsDashing() && !HavePassive)
            {
                var pos = W.GetLineFarmLocation(minionObj.Where(i => W.IsInRange(i)).ToList());
                if (pos.MinionsHit > 1)
                {
                    W.Cast(pos.Position, PacketCast);
                }
                else
                {
                    var obj =
                        minionObj.Where(i => W.GetPrediction(i).Hitchance >= HitChance.High)
                            .MinOrDefault(i => i.Distance(Player));
                    if (obj != null)
                    {
                        W.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast);
                    }
                }
            }
        }

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value || !Q.IsReady())
            {
                return;
            }
            var target = Q2.GetTarget();
            if (target == null || Q.IsInRange(target))
            {
                return;
            }
            CastExtendQ(target);
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
            if (Player.IsDashing() ||
                (!GetValue<bool>("KillSteal", "RStop") && Player.IsCastingInterruptableSpell(true)))
            {
                return;
            }
            var cancelR = GetValue<bool>("KillSteal", "RStop") && Player.IsCastingInterruptableSpell(true);
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget();
                if (target != null && Q.IsKillable(target))
                {
                    if (Q.IsInRange(target))
                    {
                        if ((!cancelR || R.Cast(PacketCast)) && Q.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                    else if (CastExtendQ(target, cancelR))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady() && !Player.IsDashing())
            {
                var target = W.GetTarget();
                if (target != null && W.IsKillable(target) && (!cancelR || R.Cast(PacketCast)))
                {
                    W.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
                }
            }
        }

        private void LockROnTarget()
        {
            var target = R.GetTarget();
            if (target == null)
            {
                return;
            }
            var endPos = (Player.ServerPosition - target.ServerPosition).Normalized();
            var predPos = R.GetPrediction(target).CastPosition.To2D();
            var fullPoint = new Vector2(predPos.X + endPos.X * R.Range * 0.98f, predPos.Y + endPos.Y * R.Range * 0.98f);
            var closestPoint = Player.ServerPosition.To2D().Closest(new List<Vector2> { predPos, fullPoint });
            if (closestPoint.IsValid() && !closestPoint.IsWall() && predPos.Distance(closestPoint) > E.Range)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, closestPoint.To3D());
            }
            else if (fullPoint.IsValid() && !fullPoint.IsWall() && predPos.Distance(fullPoint) < R.Range &&
                     predPos.Distance(fullPoint) > 100)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, fullPoint.To3D());
            }
        }

        private bool CastExtendQ(Obj_AI_Hero target, bool cancelR = false)
        {
            //var obj =
            //    GetMinion(Q.Range, MinionType.Minion, MinionTeam.NotAlly)
            //        .FirstOrDefault(i => Q2.WillHit(target, i.ServerPosition.Extend(Player.ServerPosition, -Q2.Range)));
            var obj = Q2.GetPrediction(target).CollisionObjects.FirstOrDefault();
            return obj != null && (!cancelR || R.Cast(PacketCast)) && Q.CastOnUnit(obj, PacketCast);
        }

        private double GetRDmg(Obj_AI_Hero target)
        {
            var shot = (int) (7.5 + new[] { 7.5, 9, 10.5 }[R.Level - 1] * 1 / Player.AttackDelay);
            var maxShot = new[] { 26, 30, 33 }[R.Level - 1];
            return Player.CalcDamage(
                target, Damage.DamageType.Physical,
                (new[] { 40, 50, 60 }[R.Level - 1] + 0.25 * Player.FlatPhysicalDamageMod +
                 0.1 * Player.FlatMagicDamageMod) * (shot > maxShot ? maxShot : shot));
        }
    }
}