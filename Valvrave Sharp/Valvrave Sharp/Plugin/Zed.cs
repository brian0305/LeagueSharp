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
    using LeagueSharp.SDK.Core.Wrappers.Damages;

    using SharpDX;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;
    using Orbwalker = Valvrave_Sharp.Core.Orbwalker;

    internal class Zed : Program
    {
        #region Constants

        private const float OverkillValue = 1.2f;

        #endregion

        #region Static Fields

        private static Obj_GeneralParticleEmitter deathMark;

        private static bool wCasted, rCasted;

        private static Obj_AI_Minion wShadow, rShadow;

        #endregion

        #region Constructors and Destructors

        public Zed()
        {
            Q = new Spell(SpellSlot.Q, 925);
            Q2 = new Spell(SpellSlot.Q, 925);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 280);
            R = new Spell(SpellSlot.R, 700);
            Q.SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.05f, float.MaxValue);
            Q.DamageType = Q2.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.VeryHigh;

            var orbwalkMenu = MainMenu.Add(new Menu("Orbwalk", "Orbwalk"));
            {
                orbwalkMenu.Separator("Q/E: Always On");
                orbwalkMenu.Separator("Sub Settings");
                orbwalkMenu.Bool("Ignite", "Use Ignite");
                orbwalkMenu.Bool("Item", "Use Item");
                orbwalkMenu.Separator("W Settings");
                orbwalkMenu.Bool("WNormal", "Use For Non-R Combo");
                orbwalkMenu.List("WAdv", "Use For R Combo", new[] { "OFF", "Line", "Triangle", "Mouse" }, 1);
                orbwalkMenu.List("WSwapGap", "Swap To Gap Close", new[] { "OFF", "Smart", "Always" }, 1);
                orbwalkMenu.Separator("R Settings");
                orbwalkMenu.Bool("R", "Use R");
                orbwalkMenu.Slider(
                    "RStopRange",
                    "Priorize If Ready And Distance <=",
                    (int)(R.Range + 200),
                    (int)R.Range,
                    (int)(R.Range + W.Range));
                orbwalkMenu.List("RSwapGap", "Swap To Gap Close", new[] { "OFF", "Smart", "Always" }, 1);
                orbwalkMenu.Slider("RSwapIfHpU", "Swap If Hp < (%)", 20);
                orbwalkMenu.Bool("RSwapIfKill", "Swap If Mark Can Kill Target");
                orbwalkMenu.Separator("Extra R Settings");
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    orbwalkMenu.Bool("RCast" + enemy.ChampionName, "Cast On " + enemy.ChampionName, false);
                }
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.List("Mode", "Mode", new[] { "W-E-Q", "E-Q", "Q" });
                hybridMenu.Separator("Auto Q Settings");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Slider("AutoQMpA", "If Mp >=", 100, 0, 200);
            }
            var farmMenu = MainMenu.Add(new Menu("Farm", "Farm"));
            {
                farmMenu.Bool("Q", "Use Q");
                farmMenu.Bool("E", "Use E", false);
            }
            var ksMenu = MainMenu.Add(new Menu("KillSteal", "Kill Steal"));
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("E", "Use E");
            }
            if (GameObjects.EnemyHeroes.Any())
            {
                Evade.Init();
            }
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range");
                drawMenu.Bool("W", "W Range");
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range");
                drawMenu.Bool("Target", "Target");
                drawMenu.Bool("WPos", "W Shadow");
                drawMenu.Bool("RPos", "R Shadow");
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Evade.TryEvading += TryEvading;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            GameObject.OnCreate += (sender, args) =>
                {
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
                        DelayAction.Add(4450, () => wShadow = null);
                    }
                    else if (rCasted)
                    {
                        rShadow = shadow;
                        rCasted = false;
                        wCasted = false;
                        DelayAction.Add(7450, () => rShadow = null);
                    }
                };
            GameObject.OnCreate += (sender, args) =>
                {
                    var mark = sender as Obj_GeneralParticleEmitter;
                    if (mark != null && mark.IsValid && mark.Name == "Zed_Base_R_buf_tell.troy" && rShadow.IsValid()
                        && deathMark == null)
                    {
                        deathMark = mark;
                    }
                };
            GameObject.OnDelete += (sender, args) =>
                {
                    var mark = sender as Obj_GeneralParticleEmitter;
                    if (mark != null && mark.IsValid && mark.Name == "Zed_Base_R_buf_tell.troy"
                        && deathMark.NetworkId == mark.NetworkId)
                    {
                        deathMark = null;
                    }
                };
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    if (args.SData.Name == "ZedW")
                    {
                        rCasted = false;
                        wCasted = true;
                    }
                    if (args.SData.Name == "ZedR")
                    {
                        wCasted = false;
                        rCasted = true;
                    }
                };
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
                    range += Q.Width;
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
                if (RState > 0)
                {
                    var markTarget =
                        GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget(Q.Range + range) && HaveRMark(i));
                    if (markTarget != null)
                    {
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
            =>
                R.IsReady()
                    ? (R.Instance.Name == "ZedR" ? 0 : 1)
                    : (rShadow.IsValid() && R.Instance.Name != "ZedR" ? 2 : -1);

        private static int WState => W.IsReady() ? (W.Instance.Name == "ZedW" ? 0 : 1) : -1;

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!Q.IsReady() || !MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || Player.Mana < MainMenu["Hybrid"]["AutoQMpA"])
            {
                return;
            }
            var target = Q.GetTarget();
            if (target == null)
            {
                return;
            }
            var pred = Q.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
            if (pred.Hitchance >= Q.MinHitChance)
            {
                Q.Cast(pred.CastPosition);
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
                           || (RState == 0 && Player.Distance(target) > MainMenu["Orbwalk"]["RStopRange"])
                           || HaveRMark(target) || rShadow.IsValid() || RState == -1;
            if (!Q.IsReady() || (isCombo && !canCombo))
            {
                return;
            }
            var pred = Q.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
            if (pred.Hitchance >= Q.MinHitChance)
            {
                Q.Cast(pred.CastPosition);
            }
            else if (wShadow.IsValid() && rShadow.IsValid())
            {
                var shadow = target.Distance(wShadow) < target.Distance(rShadow) ? wShadow : rShadow;
                Q2.UpdateSourcePosition(shadow.ServerPosition, shadow.ServerPosition);
                var predQ = Q2.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
                if (predQ.Hitchance >= Q2.MinHitChance)
                {
                    Q.Cast(predQ.CastPosition);
                }
            }
            else if (wShadow.IsValid())
            {
                Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                var predQ = Q2.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
                if (predQ.Hitchance >= Q2.MinHitChance)
                {
                    Q.Cast(predQ.CastPosition);
                }
            }
            else if (rShadow.IsValid())
            {
                Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                var predQ = Q2.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
                if (predQ.Hitchance >= Q2.MinHitChance)
                {
                    Q.Cast(predQ.CastPosition);
                }
            }
        }

        private static void CastW(Obj_AI_Hero target, bool isCombo = false)
        {
            if (wShadow.IsValid() || Variables.TickCount - W.LastCastAttemptT <= 500)
            {
                return;
            }
            var castPos = Vector3.Zero;
            var spellQ = new Spell(SpellSlot.Q, Q.Range + (isCombo ? W.Range : 0));
            spellQ.SetSkillshot(Q.Delay, Q.Width, Q.Speed - 100, Q.Collision, Q.Type);
            var posPred = spellQ.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall }).CastPosition;
            if (isCombo)
            {
                switch (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index)
                {
                    case 1:
                        castPos = Player.ServerPosition + (posPred - rShadow.ServerPosition).Normalized() * W.Range;
                        break;
                    case 2:
                        var subPos1 = Player.ServerPosition
                                      + (posPred - rShadow.ServerPosition).Normalized().Perpendicular() * W.Range;
                        var subPos2 = Player.ServerPosition
                                      + (rShadow.ServerPosition - posPred).Normalized().Perpendicular() * W.Range;
                        if (!subPos1.IsWall() && subPos2.IsWall())
                        {
                            castPos = subPos1;
                        }
                        else if (subPos1.IsWall() && !subPos2.IsWall())
                        {
                            castPos = subPos2;
                        }
                        else
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
                castPos = W.IsInRange(target) ? Player.ServerPosition.Extend(posPred, E.Range) : target.ServerPosition;
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
            return deathMark != null && target.Distance(deathMark) <= target.BoundingRadius;
        }

        private static void Farm()
        {
            if (MainMenu["Farm"]["Q"] && Q.IsReady())
            {
                foreach (var minion in
                    GameObjects.EnemyMinions.Where(
                        i =>
                        i.IsValidTarget(Q.Range) && i.IsMinion()
                        && (!i.InAutoAttackRange()
                                ? Q.GetHealthPrediction(i) > 0
                                : i.Health > Player.GetAutoAttackDamage(i, true))).OrderByDescending(i => i.MaxHealth))
                {
                    var pred = Q.VPrediction(
                        minion,
                        true,
                        new[]
                            {
                                CollisionableObjects.Heroes, CollisionableObjects.Minions, CollisionableObjects.YasuoWall
                            });
                    if (pred.CollisionObjects.Count > 0
                        && pred.CollisionObjects.All(i => i.NetworkId == Player.NetworkId))
                    {
                        continue;
                    }
                    if (Q.GetHealthPrediction(minion)
                        <= (pred.CollisionObjects.Count == 0
                                ? Player.GetSpellDamage(minion, SpellSlot.Q)
                                : Player.GetSpellDamage(minion, SpellSlot.Q, Damage.DamageStage.SecondForm)))
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
            }
            if (MainMenu["Farm"]["E"] && E.IsReady()
                && GameObjects.EnemyMinions.Any(
                    i =>
                    i.IsValidTarget(E.Range) && i.IsMinion() && E.GetHealthPrediction(i) > 0
                    && E.GetHealthPrediction(i) <= Player.GetSpellDamage(i, SpellSlot.E)))
            {
                E.Cast();
            }
        }

        private static List<double> GetComboDmg(Obj_AI_Hero target, bool useQ, bool useW, bool useE, bool useR)
        {
            var dmgTotal = 0d;
            var manaTotal = 0f;
            if (MainMenu["Orbwalk"]["Item"])
            {
                if (Bilgewater.IsReady)
                {
                    dmgTotal += Player.CalculateDamage(target, DamageType.Magical, 100);
                }
                if (BotRuinedKing.IsReady)
                {
                    dmgTotal += Player.CalculateDamage(
                        target,
                        DamageType.Physical,
                        Math.Max(target.MaxHealth * 0.1, 100));
                }
                if (Tiamat.IsReady)
                {
                    dmgTotal += Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage);
                }
                if (Hydra.IsReady)
                {
                    dmgTotal += Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage);
                }
            }
            if (useQ)
            {
                dmgTotal += Player.GetSpellDamage(target, SpellSlot.Q);
                manaTotal += Q.Instance.ManaCost;
            }
            if (useW)
            {
                if (useQ)
                {
                    dmgTotal += Player.GetSpellDamage(target, SpellSlot.Q) / 2;
                }
                if (WState == 0)
                {
                    manaTotal += W.Instance.ManaCost;
                }
            }
            if (useE)
            {
                dmgTotal += Player.GetSpellDamage(target, SpellSlot.E);
                manaTotal += E.Instance.ManaCost;
            }
            dmgTotal += Player.GetAutoAttackDamage(target, true) * 2;
            if (useR || HaveRMark(target))
            {
                dmgTotal += Player.CalculateDamage(
                    target,
                    DamageType.Physical,
                    new[] { 0.2, 0.35, 0.5 }[R.Level - 1] * dmgTotal + Player.TotalAttackDamage);
            }
            return new List<double> { dmgTotal * OverkillValue, manaTotal };
        }

        private static bool HaveRMark(Obj_AI_Hero target)
        {
            return target.HasBuff("zedrtargetmark");
        }

        private static void Hybrid()
        {
            var target = GetTarget;
            if (target == null)
            {
                return;
            }
            if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index == 0 && WState == 0
                && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost
                     && Player.Distance(target) < W.Range + Q.Range)
                    || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                        && Player.Distance(target) < W.Range + E.Range)))
            {
                CastW(target);
            }
            if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index < 2)
            {
                CastE();
            }
            if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index != 0 || wShadow.IsValid() || WState == -1
                || Player.Mana < Q.Instance.ManaCost + W.Instance.ManaCost)
            {
                CastQ(target);
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                foreach (var hero in
                    GameObjects.EnemyHeroes.Where(
                        i => i.IsValidTarget() && i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.Q)))
                {
                    var pred = Q.VPrediction(hero, true, new[] { CollisionableObjects.YasuoWall });
                    if (pred.Hitchance >= Q.MinHitChance)
                    {
                        if (Q.Cast(pred.CastPosition))
                        {
                            break;
                        }
                    }
                    else if (wShadow.IsValid() && rShadow.IsValid())
                    {
                        var shadow = hero.Distance(wShadow) < hero.Distance(rShadow) ? wShadow : rShadow;
                        Q2.UpdateSourcePosition(shadow.ServerPosition, shadow.ServerPosition);
                        var predQ = Q2.VPrediction(hero, true, new[] { CollisionableObjects.YasuoWall });
                        if (predQ.Hitchance >= Q2.MinHitChance && Q.Cast(predQ.CastPosition))
                        {
                            break;
                        }
                    }
                    else if (wShadow.IsValid())
                    {
                        Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                        var predQ = Q2.VPrediction(hero, true, new[] { CollisionableObjects.YasuoWall });
                        if (predQ.Hitchance >= Q2.MinHitChance && Q.Cast(predQ.CastPosition))
                        {
                            break;
                        }
                    }
                    else if (rShadow.IsValid())
                    {
                        Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                        var predQ = Q2.VPrediction(hero, true, new[] { CollisionableObjects.YasuoWall });
                        if (predQ.Hitchance >= Q2.MinHitChance && Q.Cast(predQ.CastPosition))
                        {
                            break;
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady()
                && GameObjects.EnemyHeroes.Where(
                    i => i.IsValidTarget() && i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.E))
                       .Any(
                           i =>
                           E.IsInRange(i) || (wShadow.IsValid() && wShadow.Distance(i) < E.Range)
                           || (rShadow.IsValid() && rShadow.Distance(i) < E.Range)))
            {
                E.Cast();
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

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
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
                case OrbwalkerMode.None:
                    if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                    {
                        Orbwalker.MoveOrder(Game.CursorPos);
                        if (W.IsReady())
                        {
                            W.Cast(Game.CursorPos);
                        }
                    }
                    break;
            }
            if (Orbwalker.ActiveMode != OrbwalkerMode.Orbwalk && Orbwalker.ActiveMode != OrbwalkerMode.Hybrid)
            {
                AutoQ();
            }
        }

        private static void Orbwalk()
        {
            if (RState == 1 && Player.HealthPercent < MainMenu["Orbwalk"]["RSwapIfHpU"]
                && Player.CountEnemy(Q.Range) > rShadow.CountEnemy(W.Range) && R.Cast())
            {
                return;
            }
            var target = GetTarget;
            if (target != null)
            {
                if (MainMenu["Orbwalk"]["WSwapGap"].GetValue<MenuList>().Index > 0 && WState == 1
                    && Player.Distance(target) > wShadow.Distance(target) && !target.InAutoAttackRange()
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
                        if (((target.Health + target.PhysicalShield < calcCombo[0]
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
                if (MainMenu["Orbwalk"]["RSwapGap"].GetValue<MenuList>().Index > 0 && RState == 1
                    && Player.Distance(target) > rShadow.Distance(target) && !target.InAutoAttackRange()
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
                        if (((target.Health + target.PhysicalShield < calcCombo[0]
                              && (Player.Mana >= calcCombo[1] || Player.Mana * OverkillValue >= calcCombo[1]))
                             || (MainMenu["Orbwalk"]["WNormal"] && WState == 0 && !W.IsInRange(target)
                                 && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost
                                      && rShadow.Distance(target) < W.Range + Q.Range)
                                     || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                                         && rShadow.Distance(target) < W.Range + E.Range)))) && R.Cast())
                        {
                            return;
                        }
                    }
                    else if (!Q.IsReady() && R.Cast())
                    {
                        return;
                    }
                }
                if (RState == 1 && MainMenu["Orbwalk"]["RSwapIfKill"] && DeadByRMark(target)
                    && Player.CountEnemy(Q.Range) > rShadow.CountEnemy(W.Range) && R.Cast())
                {
                    return;
                }
                if (RState == 0 && MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                    && R.IsInRange(target) && R.CastOnUnit(target))
                {
                    return;
                }
                if (MainMenu["Orbwalk"]["Ignite"] && Ignite.IsReady()
                    && (HaveRMark(target) || target.HealthPercent < 30) && Player.Distance(target) <= IgniteRange
                    && Player.Spellbook.CastSpell(Ignite, target))
                {
                    return;
                }
                if (WState == 0
                    && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost
                         && Player.Distance(target) < W.Range + Q.Range)
                        || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                            && Player.Distance(target) < W.Range + E.Range)))
                {
                    if (MainMenu["Orbwalk"]["WNormal"])
                    {
                        if (RState < 1
                            && (!MainMenu["Orbwalk"]["R"] || !MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                                || (RState == 0 && Player.Distance(target) > MainMenu["Orbwalk"]["RStopRange"])
                                || RState == -1))
                        {
                            CastW(target);
                        }
                        if (RState > 0 && MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                            && !HaveRMark(target))
                        {
                            CastW(target);
                        }
                    }
                    if (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index > 0 && RState > 0
                        && MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                        && HaveRMark(target))
                    {
                        CastW(target, true);
                    }
                }
                CastQ(target, true);
                CastE();
            }
            if (MainMenu["Orbwalk"]["Item"])
            {
                UseItem(target);
            }
        }

        private static void TryEvading(List<Skillshot> hitBy, Vector2 to)
        {
            var dangerLevel = hitBy.Select(i => i.DangerLevel).Concat(new[] { 0 }).Max();
            var zedR =
                EvadeSpellDatabase.Spells.FirstOrDefault(
                    i => i.Enable && i.DangerLevel <= dangerLevel && i.IsReady && i.Slot == SpellSlot.R);
            if (zedR == null)
            {
                return;
            }
            if (Evade.IsAboutToHit(Player, zedR.Delay - MainMenu["Evade"]["Spells"][zedR.Name]["RDelay"]))
            {
                Player.Spellbook.CastSpell(zedR.Slot);
            }
        }

        private static void UseItem(Obj_AI_Hero target)
        {
            if (target != null && (HaveRMark(target) || target.HealthPercent < 40 || Player.HealthPercent < 50))
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
            if (Youmuu.IsReady && Player.CountEnemy(R.Range + E.Range) > 0)
            {
                Youmuu.Cast();
            }
            if (Tiamat.IsReady && Player.CountEnemy(Tiamat.Range) > 0)
            {
                Tiamat.Cast();
            }
            if (Hydra.IsReady && Player.CountEnemy(Hydra.Range) > 0)
            {
                Hydra.Cast();
            }
            if (Titanic.IsReady && Player.CountEnemy(Titanic.Range) > 0)
            {
                Titanic.Cast();
            }
        }

        #endregion
    }
}