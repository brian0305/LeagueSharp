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

    using Valvrave_Sharp.Core;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal class Zed : Program
    {
        #region Constants

        private const float OverkillValue = 1.15f;

        #endregion

        #region Static Fields

        private static Obj_GeneralParticleEmitter deathMark;

        private static bool wCasted, rCasted;

        private static Obj_AI_Minion wShadow, rShadow;

        #endregion

        #region Constructors and Destructors

        public Zed()
        {
            Q = new Spell(SpellSlot.Q, 975);
            Q2 = new Spell(SpellSlot.Q, 975);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 290);
            R = new Spell(SpellSlot.R, 700);
            Q.SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0, float.MaxValue);
            Q.DamageType = Q2.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.VeryHigh;

            var orbwalkMenu = new Menu("Orbwalk", "Orbwalk");
            {
                Config.Separator(orbwalkMenu, "blank0", "Q/E/Ignite/Item: Always On");
                Config.Separator(orbwalkMenu, "blank1", "W Settings");
                Config.Bool(orbwalkMenu, "WNormal", "Use For Non-R Combo");
                Config.List(orbwalkMenu, "WAdv", "Use For R Combo", new[] { "OFF", "Line", "Triangle", "Mouse" }).Index
                    = 1;
                Config.List(orbwalkMenu, "WSwapGap", "Swap To Gap Close", new[] { "OFF", "Smart", "Always" }).Index = 1;
                Config.Separator(orbwalkMenu, "blank2", "R Settings");
                Config.Bool(orbwalkMenu, "R", "Use R");
                Config.Slider(
                    orbwalkMenu,
                    "RStopRange",
                    "Priorize If Ready And Distance <=",
                    (int)(R.Range + 200),
                    (int)R.Range,
                    (int)(R.Range + W.Range));
                Config.List(orbwalkMenu, "RSwapGap", "Swap To Gap Close", new[] { "OFF", "Smart", "Always" }).Index = 1;
                Config.Slider(orbwalkMenu, "RSwapIfHpU", "Swap If Hp < (%)", 20);
                Config.Bool(orbwalkMenu, "RSwapIfKill", "Swap If Mark Can Kill Target");
                Config.Separator(orbwalkMenu, "blank3", "Extra R Settings");
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    Config.Bool(orbwalkMenu, "RCast" + enemy.ChampionName, "Cast On " + enemy.ChampionName, false);
                }
                MainMenu.Add(orbwalkMenu);
            }
            var hybridMenu = new Menu("Hybrid", "Hybrid");
            {
                Config.List(hybridMenu, "Mode", "Mode", new[] { "W-E-Q", "E-Q", "Q" });
                Config.Bool(hybridMenu, "QOverW", "Priorize Q Over W", false);
                Config.Separator(hybridMenu, "blank4", "Auto Q Settings");
                Config.KeyBind(hybridMenu, "AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                Config.Slider(hybridMenu, "AutoQMpA", "If Mp >=", 100, 0, 200);
                MainMenu.Add(hybridMenu);
            }
            var farmMenu = new Menu("Farm", "Farm");
            {
                Config.Bool(farmMenu, "Q", "Use Q");
                Config.Bool(farmMenu, "E", "Use E", false);
                MainMenu.Add(farmMenu);
            }
            var ksMenu = new Menu("KillSteal", "Kill Steal");
            {
                Config.Bool(ksMenu, "Q", "Use Q");
                Config.Bool(ksMenu, "E", "Use E");
                MainMenu.Add(ksMenu);
            }
            var drawMenu = new Menu("Draw", "Draw");
            {
                Config.Bool(drawMenu, "Q", "Q Range");
                Config.Bool(drawMenu, "W", "W Range");
                Config.Bool(drawMenu, "E", "E Range", false);
                Config.Bool(drawMenu, "R", "R Range");
                Config.Bool(drawMenu, "Target", "Target");
                Config.Bool(drawMenu, "WPos", "W Shadow");
                Config.Bool(drawMenu, "RPos", "R Shadow");
                MainMenu.Add(drawMenu);
            }
            Config.KeyBind(MainMenu, "FleeW", "Use W To Flee", Keys.C);

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
                else
                {
                    range += 50;
                }
                var target = Q.GetTarget(range);
                if (RState == 0 && MainMenu["Orbwalk"]["R"])
                {
                    var targets =
                        GameObjects.EnemyHeroes.Where(
                            i => i.IsValidTarget(Q.Range + range) && MainMenu["Orbwalk"]["RCast" + i.ChampionName])
                            .ToList();
                    if (targets.Count > 0)
                    {
                        target = TargetSelector.GetTarget(targets);
                    }
                    var selectTarget = TargetSelector.GetSelectedTarget(Q.Range + range);
                    if (selectTarget != null && MainMenu["Orbwalk"]["RCast" + selectTarget.ChampionName])
                    {
                        target = selectTarget;
                    }
                }
                if (RState > 0 && rShadow.IsValid())
                {
                    var targets =
                        GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(Q.Range + range) && HaveRMark(i)).ToList();
                    if (targets.Count > 0)
                    {
                        var markTarget = TargetSelector.GetTarget(targets);
                        target = markTarget;
                        if (DeadByRMark(markTarget))
                        {
                            var subTarget = Q.GetTarget(
                                range,
                                false,
                                GameObjects.EnemyHeroes.Where(i => i.NetworkId == markTarget.NetworkId));
                            {
                                if (subTarget != null)
                                {
                                    target = subTarget;
                                }
                            }
                        }
                    }
                }
                return target;
            }
        }

        private static int RState
        {
            get
            {
                return R.IsReady() ? (R.Instance.Name == "zedult" ? 0 : 1) : (R.Instance.Name == "ZedR2" ? 2 : -1);
            }
        }

        private static int WState
        {
            get
            {
                return W.IsReady()
                           ? (W.Instance.Name == "ZedShadowDash" && Variables.TickCount - W.LastCastAttemptT > 500
                                  ? 0
                                  : 1)
                           : -1;
            }
        }

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || Player.Mana < MainMenu["Hybrid"]["AutoQMpA"] || !Q.IsReady())
            {
                return;
            }
            var target = Q.GetTarget();
            if (target != null)
            {
                var pred = Common.GetPrediction(Q, target, true, new[] { CollisionableObjects.YasuoWall });
                if (pred.Hitchance >= Q.MinHitChance)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }

        private static void CastE()
        {
            if (!E.IsReady())
            {
                return;
            }
            if (
                GameObjects.EnemyHeroes.Where(i => i.IsValidTarget())
                    .Any(
                        i =>
                        E.IsInRange(i) || (wShadow.IsValid() && wShadow.Distance(i) < E.Range)
                        || (rShadow.IsValid() && rShadow.Distance(i) < E.Range)))
            {
                E.Cast();
            }
        }

        private static void CastQ(Obj_AI_Hero target, bool isCombo = false)
        {
            var canCombo = !MainMenu["Orbwalk"]["R"] || !MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                           || (Player.Distance(target) > MainMenu["Orbwalk"]["RStopRange"] && RState == 0)
                           || HaveRMark(target) || rShadow.IsValid() || RState == -1;
            if (!Q.IsReady() || (isCombo && !canCombo))
            {
                return;
            }
            var pred = Common.GetPrediction(Q, target, true, new[] { CollisionableObjects.YasuoWall });
            if (pred.Hitchance >= Q.MinHitChance)
            {
                Q.Cast(pred.CastPosition);
            }
            else if (wShadow.IsValid() || rShadow.IsValid())
            {
                if (wShadow.IsValid())
                {
                    Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                    var predQ = Common.GetPrediction(Q2, target, true, new[] { CollisionableObjects.YasuoWall });
                    if (predQ.Hitchance >= Q2.MinHitChance && Q.Cast(predQ.CastPosition))
                    {
                        return;
                    }
                }
                if (rShadow.IsValid())
                {
                    Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                    var predQ = Common.GetPrediction(Q2, target, true, new[] { CollisionableObjects.YasuoWall });
                    if (predQ.Hitchance >= Q2.MinHitChance)
                    {
                        Q.Cast(predQ.CastPosition);
                    }
                }
            }
        }

        private static void CastW(Obj_AI_Hero target, bool isCombo = false)
        {
            if (WState != 0 || Variables.TickCount - W.LastCastAttemptT <= 500
                || Player.Distance(target) >= W.Range + Q.Range)
            {
                return;
            }
            var castPos = default(Vector3);
            var spellQ = new Spell(SpellSlot.Q, Q.Range + W.Range);
            spellQ.SetSkillshot(Q.Delay, Q.Width, Q.Speed + 250, Q.Collision, Q.Type);
            var posPred =
                Common.GetPrediction(spellQ, target, true, new[] { CollisionableObjects.YasuoWall }).CastPosition;
            if (isCombo)
            {
                switch (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index)
                {
                    case 1:
                        castPos = Player.ServerPosition + (posPred - rShadow.ServerPosition).Normalized() * 500;
                        break;
                    case 2:
                        var subPos1 = Player.ServerPosition
                                      + (posPred - rShadow.ServerPosition).Normalized().Perpendicular() * 500;
                        var subPos2 = Player.ServerPosition
                                      + (rShadow.ServerPosition - posPred).Normalized().Perpendicular() * 500;
                        if (subPos1.IsWall() && !subPos2.IsWall() && target.Distance(subPos2) < target.Distance(subPos1))
                        {
                            castPos = subPos2;
                        }
                        if (!subPos1.IsWall() && subPos2.IsWall() && target.Distance(subPos1) < target.Distance(subPos2))
                        {
                            castPos = subPos1;
                        }
                        if ((!subPos1.IsWall() && !subPos2.IsWall()) || (subPos1.IsWall() && subPos2.IsWall()))
                        {
                            castPos = target.Distance(subPos1) < target.Distance(subPos2) ? subPos1 : subPos2;
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
                castPos = Player.ServerPosition.Extend(posPred, E.Range);
            }
            if (!castPos.IsValid())
            {
                return;
            }
            if (W.Cast(castPos))
            {
                W.LastCastAttemptT = Variables.TickCount;
            }
        }

        private static bool DeadByRMark(Obj_AI_Hero target)
        {
            return deathMark != null && target.Distance(deathMark) < target.BoundingRadius * 1.5;
        }

        private static void Farm()
        {
            if (MainMenu["Farm"]["Q"] && Q.IsReady())
            {
                foreach (var minion in
                    GameObjects.EnemyMinions.Where(i => i.IsValidTarget(Q.Range) && Q.GetHealthPrediction(i) > 0)
                        .OrderByDescending(i => i.MaxHealth))
                {
                    var pred = Common.GetPrediction(
                        Q,
                        minion,
                        true,
                        new[]
                            {
                                CollisionableObjects.Heroes, CollisionableObjects.Minions, CollisionableObjects.YasuoWall
                            });
                    if (pred.CollisionObjects.Count > 0 && pred.CollisionObjects.Any(i => i.IsMe))
                    {
                        break;
                    }
                    if (Q.GetHealthPrediction(minion) <= GetQDmg(minion) * (pred.CollisionObjects.Count == 0 ? 1 : 0.6))
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
            }
            if (MainMenu["Farm"]["E"] && E.IsReady()
                && GameObjects.EnemyMinions.Any(
                    i =>
                    i.IsValidTarget(E.Range) && E.GetHealthPrediction(i) > 0 && E.GetHealthPrediction(i) <= GetEDmg(i)))
            {
                E.Cast();
            }
        }

        private static List<double> GetComboDmg(Obj_AI_Hero target, bool useQ, bool useW, bool useE, bool useR)
        {
            var dmgTotal = 0d;
            var manaTotal = 0f;
            if (Bilgewater.IsReady)
            {
                dmgTotal += Player.CalculateDamage(target, DamageType.Magical, 100);
            }
            if (BotRuinedKing.IsReady)
            {
                dmgTotal += Player.CalculateDamage(target, DamageType.Physical, Math.Max(target.MaxHealth * 0.1, 100));
            }
            if (Tiamat.IsReady)
            {
                dmgTotal += Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage);
            }
            if (Hydra.IsReady)
            {
                dmgTotal += Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage);
            }
            if (useQ)
            {
                dmgTotal += GetQDmg(target);
                manaTotal += Q.Instance.ManaCost;
            }
            if (useW)
            {
                if (useQ)
                {
                    dmgTotal += GetQDmg(target) / 2;
                }
                if (WState == 0)
                {
                    manaTotal += W.Instance.ManaCost;
                }
            }
            if (useE)
            {
                dmgTotal += GetEDmg(target);
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

        private static double GetEDmg(Obj_AI_Base target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                new[] { 60, 90, 120, 150, 180 }[E.Level - 1] + 0.8f * Player.FlatPhysicalDamageMod);
        }

        private static double GetQDmg(Obj_AI_Base target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                new[] { 75, 115, 155, 195, 235 }[Q.Level - 1] + Player.FlatPhysicalDamageMod);
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
            if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index == 0
                && (!MainMenu["Hybrid"]["QOverW"]
                    || (!Q.IsReady() && Player.GetLastCastedSpell().SpellData.Name != "ZedShuriken"))
                && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost)
                    || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                        && Player.Distance(target) < W.Range + E.Range)))
            {
                CastW(target);
            }
            if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index < 2)
            {
                CastE();
            }
            if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index != 0 || MainMenu["Hybrid"]["QOverW"]
                || wShadow.IsValid() || WState == -1 || Player.Mana < Q.Instance.ManaCost + W.Instance.ManaCost)
            {
                CastQ(target);
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                foreach (var hero in
                    GameObjects.EnemyHeroes.Where(i => i.IsValidTarget() && i.Health <= GetQDmg(i)))
                {
                    var pred = Common.GetPrediction(Q, hero, true, new[] { CollisionableObjects.YasuoWall });
                    if (pred.Hitchance >= Q.MinHitChance)
                    {
                        if (Q.Cast(pred.CastPosition))
                        {
                            break;
                        }
                    }
                    else if (wShadow.IsValid() || rShadow.IsValid())
                    {
                        if (wShadow.IsValid())
                        {
                            Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                            var predQ = Common.GetPrediction(Q2, hero, true, new[] { CollisionableObjects.YasuoWall });
                            if (predQ.Hitchance >= Q2.MinHitChance && Q.Cast(predQ.CastPosition))
                            {
                                break;
                            }
                        }
                        if (rShadow.IsValid())
                        {
                            Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                            var predQ = Common.GetPrediction(Q2, hero, true, new[] { CollisionableObjects.YasuoWall });
                            if (predQ.Hitchance >= Q2.MinHitChance)
                            {
                                Q.Cast(predQ.CastPosition);
                            }
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady()
                && GameObjects.EnemyHeroes.Where(i => i.IsValidTarget() && i.Health <= GetEDmg(i))
                       .Any(
                           i =>
                           E.IsInRange(i) || (wShadow.IsValid() && wShadow.Distance(i) < E.Range)
                           || (rShadow.IsValid() && rShadow.Distance(i) < E.Range)))
            {
                E.Cast();
            }
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
            if (shadow == null || !shadow.IsValid || shadow.Name != "Shadow" || shadow.IsEnemy)
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
            if (MainMenu["Draw"]["Q"] && Q.Level > 0)
            {
                Drawing.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["W"] && W.Level > 0)
            {
                Drawing.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Drawing.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["R"] && R.Level > 0)
            {
                Drawing.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["Target"])
            {
                var target = GetTarget;
                if (target != null)
                {
                    Drawing.DrawCircle(target.Position, target.BoundingRadius * 2, Color.Aqua);
                }
            }
            if (MainMenu["Draw"]["WPos"] && wShadow.IsValid())
            {
                Drawing.DrawCircle(wShadow.Position, wShadow.BoundingRadius * 2, Color.MediumSlateBlue);
                var pos = Drawing.WorldToScreen(wShadow.Position);
                Drawing.DrawText(pos.X - (float)Drawing.GetTextExtent("W").Width / 2, pos.Y, Color.BlueViolet, "W");
            }
            if (MainMenu["Draw"]["RPos"] && rShadow.IsValid())
            {
                Drawing.DrawCircle(rShadow.Position, rShadow.BoundingRadius * 2, Color.MediumSlateBlue);
                var pos = Drawing.WorldToScreen(rShadow.Position);
                Drawing.DrawText(pos.X - (float)Drawing.GetTextExtent("R").Width / 2, pos.Y, Color.BlueViolet, "R");
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "ZedShuriken" && Orbwalker.ActiveMode == OrbwalkerMode.Hybrid
                && MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index == 0 && MainMenu["Hybrid"]["QOverW"]
                && WState == 0 && GetTarget != null)
            {
                var spellQ = new Spell(SpellSlot.Q, Q.Range + W.Range);
                spellQ.SetSkillshot(Q.Delay, Q.Width, Q.Speed + 250, Q.Collision, Q.Type);
                for (var i = 0; i < 360; i += 30)
                {
                    var posRotated = Player.ServerPosition
                                     + Player.Direction.Perpendicular().Rotated((float)(Math.PI * i / 180)) * W.Range;
                    spellQ.UpdateSourcePosition(posRotated, posRotated);
                    var predQ = Common.GetPrediction(spellQ, GetTarget, true, new[] { CollisionableObjects.YasuoWall });
                    if (predQ.Hitchance < Q2.MinHitChance)
                    {
                        continue;
                    }
                    DelayAction.Add(
                        Q.Delay * 1000,
                        () =>
                            {
                                if (W.Cast(posRotated))
                                {
                                    W.LastCastAttemptT = Variables.TickCount;
                                }
                            });
                    break;
                }
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
                case OrbwalkerMode.LastHit:
                    Farm();
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
            if (RState == 1 && rShadow.IsValid() && Player.HealthPercent < MainMenu["Orbwalk"]["RSwapIfHpU"]
                && Common.CountEnemy(Q.Range) > Common.CountEnemy(W.Range, rShadow.ServerPosition) && R.Cast())
            {
                return;
            }
            var target = GetTarget;
            if (target != null)
            {
                if (MainMenu["Orbwalk"]["WSwapGap"].GetValue<MenuList>().Index > 0 && WState == 1 && wShadow.IsValid()
                    && Player.Distance(target) > wShadow.Distance(target) && !E.IsInRange(target)
                    && !DeadByRMark(target))
                {
                    if (MainMenu["Orbwalk"]["WSwapGap"].GetValue<MenuList>().Index == 1)
                    {
                        var calcCombo = GetComboDmg(
                            target,
                            Q.IsReady(),
                            wShadow.Distance(target) < Q.Range
                            || (rShadow.IsValid() && rShadow.Distance(target) < Q.Range),
                            E.IsReady(),
                            MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                            && RState == 0);
                        if (((target.Health < calcCombo[0]
                              && (Player.Mana >= calcCombo[1] || Player.Mana * OverkillValue >= calcCombo[1]))
                             || (MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                                 && RState == 0 && !R.IsInRange(target) && wShadow.Distance(target) < R.Range))
                            && W.Cast())
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
                    && Player.Distance(target) > rShadow.Distance(target) && !E.IsInRange(target)
                    && !DeadByRMark(target))
                {
                    if (MainMenu["Orbwalk"]["RSwapGap"].GetValue<MenuList>().Index == 1)
                    {
                        var calcCombo = GetComboDmg(
                            target,
                            Q.IsReady(),
                            rShadow.Distance(target) < Q.Range
                            || (wShadow.IsValid() && wShadow.Distance(target) < Q.Range),
                            E.IsReady(),
                            false);
                        if (((target.Health < calcCombo[0]
                              && (Player.Mana >= calcCombo[1] || Player.Mana * OverkillValue >= calcCombo[1]))
                             || (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index > 0 && WState == 0
                                 && !W.IsInRange(target) && rShadow.Distance(target) < W.Range + E.Range
                                 && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost)
                                     || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost))))
                            && R.Cast())
                        {
                            return;
                        }
                    }
                    else if (!Q.IsReady() && R.Cast())
                    {
                        return;
                    }
                }
                if (RState == 1 && rShadow.IsValid() && MainMenu["Orbwalk"]["RSwapIfKill"] && DeadByRMark(target)
                    && Common.CountEnemy(Q.Range) > Common.CountEnemy(W.Range, rShadow.ServerPosition) && R.Cast())
                {
                    return;
                }
                if (RState == 0 && MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                    && R.IsInRange(target) && R.CastOnUnit(target))
                {
                    return;
                }
                if (Player.IsVisible)
                {
                    if (Ignite.IsReady() && ((HaveRMark(target) && rShadow.IsValid()) || target.HealthPercent < 30)
                        && Player.Distance(target) <= 600 && Player.Spellbook.CastSpell(Ignite, target))
                    {
                        return;
                    }
                    if ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost)
                        || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                            && Player.Distance(target) < W.Range + E.Range))
                    {
                        if (MainMenu["Orbwalk"]["WNormal"])
                        {
                            if (RState < 1
                                && (!MainMenu["Orbwalk"]["R"] || !MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                                    || RState == -1))
                            {
                                CastW(target);
                            }
                            if (RState > 0 && MainMenu["Orbwalk"]["R"]
                                && MainMenu["Orbwalk"]["RCast" + target.ChampionName] && !HaveRMark(target)
                                && rShadow.IsValid())
                            {
                                CastW(target);
                            }
                        }
                        if (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index > 0 && RState > 0
                            && MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                            && HaveRMark(target) && rShadow.IsValid())
                        {
                            CastW(target, true);
                        }
                    }
                    CastQ(target, true);
                    CastE();
                }
            }
            UseItem(target);
        }

        private static void UseItem(Obj_AI_Hero target)
        {
            if (target != null
                && ((HaveRMark(target) && rShadow.IsValid()) || target.HealthPercent < 40 || Player.HealthPercent < 50))
            {
                if (Bilgewater.IsReady)
                {
                    Bilgewater.Cast(target);
                }
                if (BotRuinedKing.IsReady)
                {
                    BotRuinedKing.Cast(target);
                }
            }
            if (Youmuu.IsReady && Common.CountEnemy(R.Range + E.Range) > 0)
            {
                Youmuu.Cast();
            }
            if (Tiamat.IsReady && Common.CountEnemy(Tiamat.Range) > 0)
            {
                Tiamat.Cast();
            }
            if (Hydra.IsReady && Common.CountEnemy(Hydra.Range) > 0)
            {
                Hydra.Cast();
            }
            if (Titanic.IsReady && Common.CountEnemy(Titanic.Range) > 0)
            {
                Titanic.Cast();
            }
        }

        #endregion
    }
}