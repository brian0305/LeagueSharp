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
        private Vector3 _rEndPos;
        private bool _rKillable;
        private Obj_AI_Hero _rTarget;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 630);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 1000, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 445);
            R = new Spell(SpellSlot.R, 1400);
            Q2.SetSkillshot(0.3f, 65, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 55, 1600, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0, 55, 2800, true, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "P", "Use Passive");
                    AddItem(comboMenu, "PSave", "-> Always Save", false);
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "QExtend", "-> Extend");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "WPred", "-> Prediction", false);
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "EGap", "-> Gap Closer");
                    AddItem(comboMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
                    AddItem(comboMenu, "EMode", "-> Mode", new[] { "Safe", "Mouse", "Chase" });
                    AddItem(comboMenu, "EModeKey", "--> Key Switch", "Z", KeyBindType.Toggle).ValueChanged +=
                        ComboEModeChanged;
                    AddItem(comboMenu, "EModeDraw", "--> Draw Text", false);
                    AddItem(comboMenu, "R", "Use R If Killable");
                    AddItem(comboMenu, "RItem", "-> Use Youmuu For More Damage");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q (Only Extend)", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddItem(harassMenu, "P", "Use Passive");
                    AddItem(harassMenu, "PSave", "-> Always Save", false);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "W", "Use W");
                    AddItem(harassMenu, "E", "Use E");
                    AddItem(harassMenu, "EHpA", "-> If Hp Above", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "E", "Use E");
                    AddItem(clearMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "E", "Use E");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "RStop", "Stop R For Kill Steal");
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "W", "Use W");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    AddItem(miscMenu, "LockR", "Lock R On Target");
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
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalk.AfterAttack += AfterAttack;
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
                    if (GetValue<bool>("Flee", "E") && E.IsReady() &&
                        E.Cast(Player.ServerPosition.Extend(Game.CursorPos, E.Range), PacketCast))
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
                Utility.DelayAction.Add(500, () => _qCasted = false);
            }
            if (args.SData.Name == "LucianW")
            {
                _wCasted = true;
                Utility.DelayAction.Add(500, () => _wCasted = false);
            }
            if (args.SData.Name == "LucianE")
            {
                _eCasted = true;
                Utility.DelayAction.Add(500, () => _eCasted = false);
            }
            if (args.SData.Name == "LucianR" && !_rKillable)
            {
                _rEndPos =
                    (Player.ServerPosition -
                     (Player.ServerPosition.To2D() + R.Range * Player.Direction.To2D().Perpendicular()).To3D())
                        .Normalized();
                Utility.DelayAction.Add(3000, () => _rEndPos = new Vector3());
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
                    Player.HealthPercentage() >= GetValue<Slider>("Harass", "EHpA").Value)) && target is Obj_AI_Hero)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "E") && !HavePassive(Orbwalk.CurrentMode.ToString()))
            {
                var obj = (Obj_AI_Base) target;
                if (Orbwalk.CurrentMode == Orbwalker.Mode.Clear || Orbwalk.CurrentMode == Orbwalker.Mode.Harass ||
                    (Orbwalk.CurrentMode == Orbwalker.Mode.Combo &&
                     GetValue<StringList>("Combo", "EMode").SelectedIndex == 0))
                {
                    var pos = Geometry.CircleCircleIntersection(
                        Player.ServerPosition.To2D(), obj.ServerPosition.To2D(), E.Range,
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
                            _rTarget = target;
                            _rEndPos = (Player.ServerPosition - target.ServerPosition).Normalized();
                            _rKillable = true;
                            Utility.DelayAction.Add(
                                3000, () =>
                                {
                                    _rTarget = null;
                                    _rEndPos = new Vector3();
                                    _rKillable = false;
                                });
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
            if (GetValue<bool>(mode, "PSave") && HavePassive(mode))
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
                    if (((Orbwalk.InAutoAttackRange(target) && !HavePassive(mode)) ||
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
                    ((Orbwalk.InAutoAttackRange(target) && !HavePassive(mode)) || !Orbwalk.InAutoAttackRange(target, 20)))
                {
                    if (mode == "Harass" || GetValue<bool>(mode, "WPred"))
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
                    }
                    else if (mode == "Combo" && !GetValue<bool>(mode, "WPred"))
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
            if (!minionObj.Any() || HavePassive())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && (E.IsReady() || E.IsReady(GetValue<Slider>("Clear", "EDelay").Value)))
            {
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() && !Player.IsDashing())
            {
                var pos = W.GetCircularFarmLocation(minionObj.Where(i => W.IsInRange(i)).ToList());
                if (pos.MinionsHit > 1)
                {
                    if (W.Cast(pos.Position, PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    var obj = minionObj.FirstOrDefault(i => i.MaxHealth >= 1200);
                    if (obj != null && W.IsInRange(obj) && W.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast))
                    {
                        return;
                    }
                }
            }
            if ((!GetValue<bool>("Clear", "W") || !W.IsReady()) && GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var pos = Q2.GetLineFarmLocation(minionObj);
                if (pos.MinionsHit > 0)
                {
                    var obj =
                        minionObj.FirstOrDefault(
                            i =>
                                Q.IsInRange(i) &&
                                Q2.WillHit(
                                    i, pos.Position.To3D().Extend(Player.ServerPosition, -Q2.Range),
                                    (int) (i.BoundingRadius / 2)));
                    if (obj != null && Q.CastOnUnit(obj, PacketCast))
                    {
                        Utility.DelayAction.Add(300, () => Player.IssueOrder(GameObjectOrder.AttackUnit, obj));
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
            var target = _rTarget.IsValidTarget() ? _rTarget : R.GetTarget();
            if (target == null)
            {
                return;
            }
            var pos = R.GetPrediction(target).CastPosition;
            var fullPoint =
                new Vector2(pos.X + _rEndPos.X * R.Range * 0.98f, pos.Y + _rEndPos.Y * R.Range * 0.98f).To3D();
            //var MidPoint = new Vector2((FullPoint.X * 2 - Pos.X) / Pos.Distance(FullPoint) * R.Range * 0.98f, (FullPoint.Y * 2 - Pos.Y) / Pos.Distance(FullPoint) * R.Range * 0.98f).To3D();
            var closestPoint = Player.ServerPosition.To2D().Closest(new List<Vector3> { pos, fullPoint }.To2D()).To3D();
            if (closestPoint.IsValid() && !closestPoint.IsWall() && pos.Distance(closestPoint) > E.Range)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, closestPoint);
            }
            else if (fullPoint.IsValid() && !fullPoint.IsWall() && pos.Distance(fullPoint) < R.Range &&
                     pos.Distance(fullPoint) > 100)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, fullPoint);
            }
            //else if (MidPoint.IsValid() && !MidPoint.IsWall()) Player.IssueOrder(GameObjectOrder.MoveTo, MidPoint);
        }

        private bool CastExtendQ(Obj_AI_Hero target, bool cancelR = false)
        {
            var obj =
                MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                    .FirstOrDefault(
                        i =>
                            Q2.WillHit(
                                target, i.ServerPosition.Extend(Player.ServerPosition, -Q2.Range),
                                (int) (target.BoundingRadius / 2)));
            return obj != null && (!cancelR || R.Cast(PacketCast)) && Q.CastOnUnit(obj, PacketCast);
        }

        private bool HavePassive(string mode = "Clear")
        {
            return (mode == "Clear" || GetValue<bool>(mode, "P")) &&
                   (_qCasted || _wCasted || _eCasted || Player.HasBuff("LucianPassiveBuff"));
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