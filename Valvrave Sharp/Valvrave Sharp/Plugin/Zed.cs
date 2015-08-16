namespace Valvrave_Sharp.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

    using SharpDX;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal class Zed : AddUI
    {
        #region Constants

        private const float OverkillValue = 1.2f;

        #endregion

        #region Static Fields

        private static Obj_GeneralParticleEmitter deathMark;

        private static bool eForced;

        private static bool wCasted, rCasted;

        private static Obj_AI_Minion wShadow, rShadow;

        #endregion

        #region Constructors and Destructors

        public Zed()
        {
            Q = new Spell(SpellSlot.Q, 925);
            Q2 = new Spell(SpellSlot.Q, 925);
            W = new Spell(SpellSlot.W, 550);
            E = new Spell(SpellSlot.E, 290);
            R = new Spell(SpellSlot.R, 625);
            Q.SetSkillshot(0.25f, 50, 1700, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 50, 1700, false, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.High;

            var orbwalkMenu = new Menu("Orbwalk", "Orbwalk");
            {
                Separator(orbwalkMenu, "blank0", "Q/E/Ignite/Item: Always ON");
                Separator(orbwalkMenu, "blank1", "W Settings");
                Bool(orbwalkMenu, "WNormal", "Use For Non-R Combo");
                List(orbwalkMenu, "WAdv", "Use For R Combo", new[] { "Off", "Line", "Triangle", "Mouse" });
                List(orbwalkMenu, "WSwapGap", "Swap To Gap Close", new[] { "Off", "Smart", "Always" });
                Separator(orbwalkMenu, "blank2", "R Settings");
                Bool(orbwalkMenu, "R", "Use R");
                Slider(
                    orbwalkMenu,
                    "RStopRange",
                    "Priorize If Ready And Distance <=",
                    (int)(R.Range * 1.5),
                    (int)R.Range,
                    (int)(R.Range + W.Range));
                List(orbwalkMenu, "RSwapGap", "Swap To Gap Close", new[] { "Off", "Smart", "Always" });
                Slider(orbwalkMenu, "RSwapIfHpU", "Swap If Hp < (%)", 20);
                Bool(orbwalkMenu, "RSwapIfKill", "Swap If Mark Can Kill Target");
                Separator(orbwalkMenu, "blank3", "Extra R Settings");
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    Bool(orbwalkMenu, "RCast" + enemy.ChampionName, "Cast On " + enemy.ChampionName);
                }
                MainMenu.Add(orbwalkMenu);
            }
            var hybridMenu = new Menu("Hybrid", "Hybrid");
            {
                List(hybridMenu, "Mode", "Mode", new[] { "W-E-Q", "E-Q", "Q" });
                Separator(hybridMenu, "blank4", "Auto Q Settings");
                KeyBind(hybridMenu, "AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                Slider(hybridMenu, "AutoQMpA", "If Mp >=", 100, 0, 200);
                MainMenu.Add(hybridMenu);
            }
            var ksMenu = new Menu("KillSteal", "Kill Steal");
            {
                Bool(ksMenu, "Q", "Use Q");
                Bool(ksMenu, "E", "Use E");
                //MainMenu.Add(ksMenu);
            }
            var drawMenu = new Menu("Draw", "Draw");
            {
                Bool(drawMenu, "Target", "Target");
                Bool(drawMenu, "WPos", "W Shadow");
                Bool(drawMenu, "RPos", "R Shadow");
                MainMenu.Add(drawMenu);
            }
            KeyBind(MainMenu, "FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
        }

        #endregion

        #region Properties

        private static Obj_AI_Hero GetTarget
        {
            get
            {
                float range = 0;
                if (wShadow.IsValid() && rShadow.IsValid())
                {
                    range += Math.Max(Player.Distance(rShadow), Player.Distance(wShadow));
                }
                else if (WState == 0 && rShadow.IsValid())
                {
                    range += Math.Max(Player.Distance(rShadow), W.Range);
                }
                else if (wShadow.IsValid())
                {
                    range += Player.Distance(wShadow);
                }
                else if (WState == 0)
                {
                    range += W.Range;
                }
                var target = Q.GetTarget(range);
                switch (RState)
                {
                    case 0:
                        if (MainMenu["Orbwalk"]["R"].GetValue<MenuBool>().Value)
                        {
                            var subTarget = Q.GetTarget(
                                range,
                                false,
                                GameObjects.EnemyHeroes.Where(
                                    i => !MainMenu["Orbwalk"]["RCast" + i.ChampionName].GetValue<MenuBool>().Value));
                            if (subTarget != null)
                            {
                                target = subTarget;
                            }
                        }
                        break;
                    case 1:
                        var markTarget = Q.GetTarget(range, false, GameObjects.EnemyHeroes.Where(i => !HaveRMark(i)));
                        if (markTarget != null)
                        {
                            target = markTarget;
                        }
                        if (target != null && DeadByRMark(target))
                        {
                            var subTarget = Q.GetTarget(
                                range,
                                false,
                                GameObjects.EnemyHeroes.Where(i => i.Compare(target)));
                            if (subTarget != null)
                            {
                                target = subTarget;
                            }
                        }
                        break;
                }
                return target;
            }
        }

        private static int RState
        {
            get
            {
                if (R.IsReady())
                {
                    return R.Instance.Name == "zedult" ? 0 : 1;
                }
                return R.Instance.Name == "zedr2" ? 2 : -1;
            }
        }

        private static int WState
        {
            get
            {
                if (W.IsReady())
                {
                    return W.Instance.Name == "ZedShadowDash" ? 0 : 1;
                }
                return -1;
            }
        }

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || Player.Mana < MainMenu["Hybrid"]["AutoQMpA"].GetValue<MenuSlider>().Value || !Q.IsReady())
            {
                return;
            }
            Q.CastOnBestTarget();
        }

        private static bool CanCast(Obj_AI_Hero target)
        {
            if (WState != 0)
            {
                return WState == -1 || wShadow.IsValid();
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Hybrid:
                    return true;
                case OrbwalkerMode.Orbwalk:
                    if (!MainMenu["Orbwalk"]["WNormal"].GetValue<MenuBool>().Value)
                    {
                        return true;
                    }
                    if (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index == 0)
                    {
                        return true;
                    }
                    var calcCombo = GetComboDmg(
                        target,
                        Q.IsReady(),
                        true,
                        E.IsReady(),
                        MainMenu["Orbwalk"]["R"].GetValue<MenuBool>().Value
                        && MainMenu["Orbwalk"]["RCast" + target.ChampionName].GetValue<MenuBool>().Value && RState == 0);
                    if ((target.Health >= calcCombo[0] || Player.Mana <= calcCombo[1]
                         || Player.Mana * OverkillValue <= calcCombo[1])
                        && Player.Distance(target) <= MainMenu["Orbwalk"]["RStopRange"].GetValue<MenuSlider>().Value
                        && !HaveRMark(target))
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private static void CastE(Obj_AI_Hero target, bool isCombo = false)
        {
            var canCombo = (!MainMenu["Orbwalk"]["RCast" + target.ChampionName].GetValue<MenuBool>().Value
                            || (Player.Distance(target) > MainMenu["Orbwalk"]["RStopRange"].GetValue<MenuSlider>().Value
                                && RState == 0)) || rShadow.IsValid() || HaveRMark(target) || RState == -1;
            if (!E.IsReady() || (isCombo && !canCombo) || !CanCast(target))
            {
                return;
            }
            if (E.IsInRange(target) && E.Cast())
            {
                return;
            }
            if (wShadow.IsValid() && wShadow.Distance(target) < E.Range && E.Cast())
            {
                return;
            }
            if (rShadow.IsValid() && rShadow.Distance(target) < E.Range)
            {
                E.Cast();
            }
        }

        private static void CastQ(Obj_AI_Hero target, bool isCombo = false)
        {
            var canCombo = (!MainMenu["Orbwalk"]["RCast" + target.ChampionName].GetValue<MenuBool>().Value
                            || (Player.Distance(target) > MainMenu["Orbwalk"]["RStopRange"].GetValue<MenuSlider>().Value
                                && RState == 0)) || rShadow.IsValid() || HaveRMark(target) || RState == -1;
            if (!Q.IsReady() || (isCombo && !canCombo) || !CanCast(target))
            {
                return;
            }
            if (Q.Cast(target) == CastStates.SuccessfullyCasted)
            {
                return;
            }
            if (!wShadow.IsValid() && !rShadow.IsValid())
            {
                return;
            }
            if (wShadow.IsValid())
            {
                Q2.UpdateSourcePosition(wShadow.Position, wShadow.Position);
                var qPred = Q2.GetPrediction(target);
                if (qPred.Hitchance >= Q2.MinHitChance && Q.Cast(qPred.CastPosition))
                {
                    return;
                }
            }
            if (rShadow.IsValid())
            {
                Q2.UpdateSourcePosition(rShadow.Position, rShadow.Position);
                var qPred = Q2.GetPrediction(target);
                if (qPred.Hitchance >= Q2.MinHitChance)
                {
                    Q.Cast(qPred.CastPosition);
                }
            }
        }

        private static void CastW(Obj_AI_Hero target, bool isCombo = false)
        {
            var canCombo = (!MainMenu["Orbwalk"]["RCast" + target.ChampionName].GetValue<MenuBool>().Value
                            || (Player.Distance(target) > MainMenu["Orbwalk"]["RStopRange"].GetValue<MenuSlider>().Value
                                && RState == 0)) || rShadow.IsValid() || HaveRMark(target) || RState == -1;
            if (WState != 0 || Variables.TickCount - W.LastCastAttemptT <= 300
                || Player.Distance(target) >= W.Range + Q.Range || (isCombo && !canCombo))
            {
                return;
            }
            var castPos = default(Vector3);
            if (rShadow.IsValid())
            {
                switch (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index)
                {
                    case 0:
                        castPos = target.ServerPosition.Extend(Player.ServerPosition, -E.Range);
                        break;
                    case 1:
                        castPos = target.ServerPosition.Extend(Player.ServerPosition, -W.Range);
                        break;
                    case 2:
                        var subPos1 = Player.ServerPosition
                                      + (target.ServerPosition - rShadow.Position).Normalized().Perpendicular()
                                      * W.Range;
                        var subPos2 = Player.ServerPosition
                                      + (rShadow.Position - target.ServerPosition).Normalized().Perpendicular()
                                      * W.Range;
                        if (subPos1.IsWall() && !subPos2.IsWall() && target.Distance(subPos2) < target.Distance(subPos1))
                        {
                            castPos = subPos2;
                        }
                        if (!subPos1.IsWall() && subPos2.IsWall() && target.Distance(subPos1) < target.Distance(subPos2))
                        {
                            castPos = subPos1;
                        }
                        if (!castPos.IsValid())
                        {
                            castPos = subPos1;
                        }
                        break;
                    case 3:
                        castPos = Game.CursorPos;
                        break;
                }
            }
            else
            {
                castPos = RState == -1
                              ? target.ServerPosition
                              : target.ServerPosition.Extend(Player.ServerPosition, -E.Range);
            }
            if (!castPos.IsWall() && W.Cast(castPos))
            {
                return;
            }
            for (var i = W.Range; i >= 400; i -= 50)
            {
                var castPosSub = Player.ServerPosition.Extend(castPos, i);
                if (!castPosSub.IsWall() && W.Cast(castPosSub))
                {
                    break;
                }
            }
        }

        private static int CountEnemy(float range, Vector3 pos = default(Vector3))
        {
            return GameObjects.EnemyHeroes.Count(i => i.IsValidTarget(range, true, pos));
        }

        private static bool DeadByRMark(Obj_AI_Hero target)
        {
            return deathMark != null && target.Distance(deathMark) < target.BoundingRadius;
        }

        private static List<double> GetComboDmg(Obj_AI_Hero target, bool useQ, bool useW, bool useE, bool useR)
        {
            var dmgQ = new[] { 75, 115, 155, 195, 235 }[Q.Level - 1] + Player.FlatPhysicalDamageMod;
            var dmgTotal = 0d;
            var manaTotal = 0f;
            if (useQ)
            {
                dmgTotal += Player.CalculateDamage(target, DamageType.Physical, dmgQ);
                manaTotal += Q.Instance.ManaCost;
            }
            if (useW)
            {
                if (useQ)
                {
                    dmgTotal += Player.CalculateDamage(target, DamageType.Physical, dmgQ / 2);
                }
                if (WState == 0)
                {
                    manaTotal += W.Instance.ManaCost;
                }
            }
            if (useE)
            {
                dmgTotal += Player.CalculateDamage(
                    target,
                    DamageType.Physical,
                    new[] { 60, 90, 120, 150, 180 }[E.Level - 1] + 0.8f * Player.FlatPhysicalDamageMod);
                manaTotal += E.Instance.ManaCost;
            }
            dmgTotal += Player.GetAutoAttackDamage(target, true);
            if (target.HealthPercent <= 50 && !target.HasBuff("ZedPassiveCD"))
            {
                dmgTotal += Player.CalculateDamage(
                    target,
                    DamageType.Magical,
                    target.MaxHealth * (Player.Level > 16 ? 0.1 : (Player.Level > 6 ? 0.08 : 0.06)));
            }
            if (useR || HaveRMark(target))
            {
                dmgTotal += new[] { 0.2, 0.35, 0.5 }[R.Level - 1] * dmgTotal + Player.TotalAttackDamage;
            }
            return new List<double> { dmgTotal * OverkillValue, manaTotal };
        }

        private static bool HaveRMark(Obj_AI_Hero target)
        {
            return target.HasBuff("zedulttargetmark");
        }

        private static void Hybrid()
        {
            var target = GetTarget;
            if (target == null)
            {
                return;
            }
            if (Orbwalker.CanMove && (!Orbwalker.CanAttack || Orbwalker.GetTarget(OrbwalkerMode.Orbwalk) == null))
            {
                if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index == 0
                    && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost)
                        || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost)))
                {
                    CastW(target);
                }
                if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index < 2)
                {
                    CastE(target);
                }
                if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index != 0 || wShadow.IsValid() || !W.IsReady()
                    || Player.Mana < Q.Instance.ManaCost + W.Instance.ManaCost)
                {
                    CastQ(target);
                }
            }
        }

        private static void KillSteal()
        {
            /*if (MainMenu["KillSteal"]["Q"].GetValue<MenuBool>().Value && Q.IsReady())
            {
            }
            if (MainMenu["KillSteal"]["E"].GetValue<MenuBool>().Value && E.IsReady())
            {
            }*/
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var mark = sender as Obj_GeneralParticleEmitter;
            if (mark != null && mark.IsValid && mark.Name == "Zed_Base_R_buf_tell.troy" && rShadow.IsValid()
                && deathMark == null)
            {
                deathMark = mark;
            }
            var shadow = sender as Obj_AI_Minion;
            if (shadow == null || !shadow.IsValid() || shadow.Name != "Shadow" || shadow.IsEnemy)
            {
                return;
            }
            if (wCasted)
            {
                wShadow = shadow;
                wCasted = false;
                rCasted = false;
                DelayAction.Add(4500, () => wShadow = null);
            }
            else if (rCasted)
            {
                rShadow = shadow;
                rCasted = false;
                wCasted = false;
                DelayAction.Add(7500, () => rShadow = null);
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            var mark = sender as Obj_GeneralParticleEmitter;
            if (mark != null && mark.IsValid && mark.Name == "Zed_Base_R_buf_tell.troy" && deathMark != null)
            {
                deathMark = null;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (MainMenu["Draw"]["Target"].GetValue<MenuBool>().Value)
            {
                var target = GetTarget;
                if (target != null)
                {
                    Drawing.DrawCircle(target.Position, target.BoundingRadius * 1.5f, Color.Aqua);
                }
            }
            if (MainMenu["Draw"]["WPos"].GetValue<MenuBool>().Value && wShadow.IsValid())
            {
                Drawing.DrawCircle(wShadow.Position, wShadow.BoundingRadius * 1.5f, Color.White);
                var pos = Drawing.WorldToScreen(wShadow.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Red, "W");
            }
            if (MainMenu["Draw"]["RPos"].GetValue<MenuBool>().Value && rShadow.IsValid())
            {
                Drawing.DrawCircle(rShadow.Position, rShadow.BoundingRadius * 1.5f, Color.White);
                var pos = Drawing.WorldToScreen(rShadow.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Red, "R");
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "ZedPBAOEDummy" && eForced)
            {
                eForced = false;
            }
            if (args.SData.Name == "ZedShadowDash")
            {
                rCasted = false;
                wCasted = true;
            }
            if (args.SData.Name == "zedult")
            {
                wCasted = false;
                rCasted = true;
                eForced = true;
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Orbwalk:
                    Orbwalk();
                    break;
                case OrbwalkerMode.Hybrid:
                    Hybrid();
                    break;
            }
            if (Orbwalker.ActiveMode != OrbwalkerMode.Orbwalk && Orbwalker.ActiveMode != OrbwalkerMode.Hybrid)
            {
                AutoQ();
            }
            if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                if (W.IsReady())
                {
                    W.Cast(Game.CursorPos);
                }
            }
        }

        private static void Orbwalk()
        {
            if (RState == 1 && rShadow.IsValid()
                && Player.HealthPercent < MainMenu["Orbwalk"]["RSwapIfHpU"].GetValue<MenuSlider>().Value
                && CountEnemy(W.Range) > CountEnemy(300, rShadow.Position) && R.Cast())
            {
                return;
            }
            if (eForced && E.IsReady() && CountEnemy(E.Range) > 0 && E.Cast())
            {
                return;
            }
            var target = GetTarget;
            if (target == null)
            {
                return;
            }
            if (MainMenu["Orbwalk"]["WSwapGap"].GetValue<MenuList>().Index > 0 && WState == 1 && wShadow.IsValid()
                && Player.Distance(target) > wShadow.Distance(target) && !E.IsInRange(target) && !DeadByRMark(target))
            {
                if (MainMenu["Orbwalk"]["WSwapGap"].GetValue<MenuList>().Index == 1)
                {
                    var calcCombo = GetComboDmg(
                        target,
                        Q.IsReady(),
                        true,
                        E.IsReady(),
                        MainMenu["Orbwalk"]["R"].GetValue<MenuBool>().Value
                        && MainMenu["Orbwalk"]["RCast" + target.ChampionName].GetValue<MenuBool>().Value && RState == 0);
                    if (((target.Health < calcCombo[0]
                          && (Player.Mana >= calcCombo[1] || Player.Mana * OverkillValue >= calcCombo[1]))
                         || (MainMenu["Orbwalk"]["R"].GetValue<MenuBool>().Value
                             && MainMenu["Orbwalk"]["RCast" + target.ChampionName].GetValue<MenuBool>().Value
                             && RState == 0 && !R.IsInRange(target) && wShadow.Distance(target) < R.Range)) && W.Cast())
                    {
                        return;
                    }
                }
                else if (!Q.IsReady() && W.Cast())
                {
                    return;
                }
            }
            if (MainMenu["Orbwalk"]["RSwapGap"].GetValue<MenuList>().Index > 0 && RState == 1 && rShadow.IsValid()
                && Player.Distance(target) > rShadow.Distance(target) && !E.IsInRange(target) && !DeadByRMark(target))
            {
                if (MainMenu["Orbwalk"]["RSwapGap"].GetValue<MenuList>().Index == 1)
                {
                    var calcCombo = GetComboDmg(
                        target,
                        Q.IsReady(),
                        wShadow.IsValid() || WState == 0,
                        E.IsReady(),
                        false);
                    if (((target.Health < calcCombo[0]
                          && (Player.Mana >= calcCombo[1] || Player.Mana * OverkillValue >= calcCombo[1]))
                         || (MainMenu["Orbwalk"]["WNormal"].GetValue<MenuBool>().Value && WState == 0
                             && !W.IsInRange(target) && rShadow.Distance(target) < W.Range)) && R.Cast())
                    {
                        return;
                    }
                }
                else if (!Q.IsReady() && R.Cast())
                {
                    return;
                }
            }
            if (RState == 1 && rShadow.IsValid() && MainMenu["Orbwalk"]["RSwapIfKill"].GetValue<MenuBool>().Value
                && DeadByRMark(target) && CountEnemy(Q.Range) > CountEnemy(400, rShadow.Position) && R.Cast())
            {
                return;
            }
            if (RState == 0 && MainMenu["Orbwalk"]["R"].GetValue<MenuBool>().Value && R.IsInRange(target)
                && (Q.IsReady() || E.IsReady() || target.HealthPercent < 40)
                && MainMenu["Orbwalk"]["RCast" + target.ChampionName].GetValue<MenuBool>().Value && R.CastOnUnit(target))
            {
                return;
            }
            if (Orbwalker.CanMove && (!Orbwalker.CanAttack || Orbwalker.GetTarget(OrbwalkerMode.Orbwalk) == null))
            {
                if (((MainMenu["Orbwalk"]["WNormal"].GetValue<MenuBool>().Value && RState < 1)
                     || (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index > 0 && RState > -1))
                    && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost)
                        || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost)
                        || !W.IsInRange(target)))
                {
                    CastW(target, true);
                }
                CastE(target, true);
                if ((MainMenu["Orbwalk"]["WNormal"].GetValue<MenuBool>().Value
                     || MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index > 0)
                    && (wShadow.IsValid() || !W.IsReady() || Player.Mana < Q.Instance.ManaCost + W.Instance.ManaCost))
                {
                    CastQ(target, true);
                }
                else if (!MainMenu["Orbwalk"]["WNormal"].GetValue<MenuBool>().Value
                         && MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index == 0)
                {
                    CastQ(target, true);
                }
                if (Ignite.IsReady() && ((HaveRMark(target) && rShadow.IsValid()) || target.HealthPercent < 30)
                    && Player.Distance(target) <= 600)
                {
                    Player.Spellbook.CastSpell(Ignite, target);
                }
                if ((HaveRMark(target) && rShadow.IsValid()) || target.HealthPercent < 40)
                {
                    if (Bilgewater.IsReady)
                    {
                        Bilgewater.Cast(target);
                    }
                    if (BotRuinedKing.IsReady)
                    {
                        BotRuinedKing.Cast(target);
                    }
                    if (Youmuu.IsReady && CountEnemy(R.Range) > 0)
                    {
                        Youmuu.Cast();
                    }
                    if (Tiamat.IsReady && CountEnemy(Tiamat.Range) > 0)
                    {
                        Tiamat.Cast();
                    }
                    if (Hydra.IsReady && CountEnemy(Hydra.Range) > 0)
                    {
                        Hydra.Cast();
                    }
                }
            }
        }

        #endregion
    }
}