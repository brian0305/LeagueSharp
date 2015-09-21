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
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

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
            Q = new Spell(SpellSlot.Q, 975);
            Q2 = new Spell(SpellSlot.Q, 975);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 290);
            R = new Spell(SpellSlot.R, 700);
            Q.SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.05f, float.MaxValue);
            Q.DamageType = Q2.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.VeryHigh;

            var orbwalkMenu = new Menu("Orbwalk", "Orbwalk");
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
                MainMenu.Add(orbwalkMenu);
            }
            var hybridMenu = new Menu("Hybrid", "Hybrid");
            {
                hybridMenu.List("Mode", "Mode", new[] { "W-E-Q", "E-Q", "Q" });
                hybridMenu.Separator("Auto Q Settings");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Slider("AutoQMpA", "If Mp >=", 100, 0, 200);
                MainMenu.Add(hybridMenu);
            }
            var farmMenu = new Menu("Farm", "Farm");
            {
                farmMenu.Bool("Q", "Use Q");
                farmMenu.Bool("E", "Use E", false);
                MainMenu.Add(farmMenu);
            }
            var ksMenu = new Menu("KillSteal", "Kill Steal");
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("E", "Use E");
                MainMenu.Add(ksMenu);
            }
            if (GameObjects.EnemyHeroes.Any())
            {
                Evade.Init();
            }
            var drawMenu = new Menu("Draw", "Draw");
            {
                drawMenu.Bool("Q", "Q Range");
                drawMenu.Bool("W", "W Range");
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range");
                drawMenu.Bool("Target", "Target");
                drawMenu.Bool("WPos", "W Shadow");
                drawMenu.Bool("RPos", "R Shadow");
                MainMenu.Add(drawMenu);
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdateEvade;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += (sender, args) =>
                {
                    var mark = sender as Obj_GeneralParticleEmitter;
                    if (mark != null && mark.IsValid && mark.Name == "Zed_Base_R_buf_tell.troy"
                        && deathMark.Compare(mark))
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
        {
            get
            {
                return R.IsReady()
                           ? (R.Instance.Name == "ZedR" ? 0 : 1)
                           : (rShadow.IsValid() && R.Instance.Name != "ZedR" ? 2 : -1);
            }
        }

        private static int WState
        {
            get
            {
                return W.IsReady() ? (W.Instance.Name == "ZedW" ? 0 : 1) : -1;
            }
        }

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
            else if (wShadow.IsValid() || rShadow.IsValid())
            {
                if (wShadow.IsValid())
                {
                    Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                    var predQ = Q2.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
                    if (predQ.Hitchance >= Q2.MinHitChance && Q.Cast(predQ.CastPosition))
                    {
                        return;
                    }
                }
                if (rShadow.IsValid())
                {
                    Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                    var predQ = Q2.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
                    if (predQ.Hitchance >= Q2.MinHitChance)
                    {
                        Q.Cast(predQ.CastPosition);
                    }
                }
            }
        }

        private static void CastW(Obj_AI_Hero target, bool isCombo = false)
        {
            if (wShadow.IsValid() || Variables.TickCount - W.LastCastAttemptT <= 500)
            {
                return;
            }
            var castPos = default(Vector3);
            var spellQ = new Spell(SpellSlot.Q, Q.Range);
            spellQ.SetSkillshot(Q.Delay, Q.Width, Q.Speed - 100, Q.Collision, Q.Type);
            var posPred = spellQ.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall }).CastPosition;
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
                        if (!castPos.IsValid())
                        {
                            if (!subPos1.IsWall() && !subPos2.IsWall())
                            {
                                castPos = target.Distance(subPos1) < target.Distance(subPos2) ? subPos1 : subPos2;
                            }
                            else
                            {
                                castPos = Player.ServerPosition + (posPred - rShadow.ServerPosition).Normalized() * 500;
                            }
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
                                : i.Health > Player.GetAutoAttackDamage(i, true) + GetPDmg(i)))
                        .OrderByDescending(i => i.MaxHealth))
                {
                    var pred = Q.VPrediction(
                        minion,
                        true,
                        new[]
                            {
                                CollisionableObjects.Heroes, CollisionableObjects.Minions, CollisionableObjects.YasuoWall
                            });
                    if (pred.CollisionObjects.Count > 0 && pred.CollisionObjects.Any(i => i.IsMe))
                    {
                        continue;
                    }
                    if (Q.GetHealthPrediction(minion) + minion.PhysicalShield
                        <= GetQDmg(minion) * (pred.CollisionObjects.Count == 0 ? 1 : 0.6))
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
            }
            if (MainMenu["Farm"]["E"] && E.IsReady()
                && GameObjects.EnemyMinions.Any(
                    i =>
                    i.IsValidTarget(E.Range) && i.IsMinion() && E.GetHealthPrediction(i) > 0
                    && E.GetHealthPrediction(i) + i.PhysicalShield <= GetEDmg(i)))
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
            dmgTotal += Player.GetAutoAttackDamage(target, true) * 2 + GetPDmg(target);
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

        private static double GetPDmg(Obj_AI_Base target)
        {
            return target.HealthPercent <= 50 && !target.HasBuff("ZedPassiveCD")
                       ? Player.CalculateDamage(
                           target,
                           DamageType.Magical,
                           target.MaxHealth * (Player.Level > 16 ? 0.1 : (Player.Level > 6 ? 0.08 : 0.06)))
                       : 0;
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
                    GameObjects.EnemyHeroes.Where(i => i.IsValidTarget() && i.Health + i.PhysicalShield <= GetQDmg(i)))
                {
                    var pred = Q.VPrediction(hero, true, new[] { CollisionableObjects.YasuoWall });
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
                            var predQ = Q2.VPrediction(hero, true, new[] { CollisionableObjects.YasuoWall });
                            if (predQ.Hitchance >= Q2.MinHitChance && Q.Cast(predQ.CastPosition))
                            {
                                break;
                            }
                        }
                        if (rShadow.IsValid())
                        {
                            Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                            var predQ = Q2.VPrediction(hero, true, new[] { CollisionableObjects.YasuoWall });
                            if (predQ.Hitchance >= Q2.MinHitChance)
                            {
                                Q.Cast(predQ.CastPosition);
                            }
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady()
                && GameObjects.EnemyHeroes.Where(i => i.IsValidTarget() && i.Health + i.PhysicalShield <= GetEDmg(i))
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

        private static void OnUpdateEvade(EventArgs args)
        {
            if (Player.IsDead || !MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active)
            {
                return;
            }
            if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
            {
                return;
            }
            var zedR = EvadeSpellDatabase.Spells.FirstOrDefault(i => i.Enabled && i.IsReady && i.Slot == SpellSlot.R);
            if (zedR != null)
            {
                var skillshot =
                    Evade.DetectedSkillshots.Where(
                        i =>
                        i.Enabled && zedR.DangerLevel <= i.DangerLevel
                        && i.IsAboutToHit(150 + zedR.Delay - MainMenu["Evade"]["Spells"][zedR.Name]["RDelay"], Player))
                        .MaxOrDefault(i => i.DangerLevel);
                if (skillshot != null)
                {
                    Player.Spellbook.CastSpell(zedR.Slot);
                }
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
                    && (HaveRMark(target) || target.HealthPercent < 30) && Player.Distance(target) <= 600
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
                        && HaveRMark(target) && Q.IsInRange(target))
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