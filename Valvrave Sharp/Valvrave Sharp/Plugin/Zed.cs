namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers.Damages;
    using LeagueSharp.SDK.Modes;

    using SharpDX;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;
    using Skillshot = Valvrave_Sharp.Evade.Skillshot;

    #endregion

    internal class Zed : Program
    {
        #region Constants

        private const CollisionableObjects QColObjects =
            CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall;

        private const int RangeW = 700;

        private const string RMarkName = "Zed_Base_R_buf_tell.troy";

        private const string ShadowName = "zedshadow";

        #endregion

        #region Static Fields

        private static int lastW;

        private static GameObject rMarkObject;

        private static Obj_AI_Minion wShadow, rShadow;

        #endregion

        #region Constructors and Destructors

        public Zed()
        {
            Q = new Spell(SpellSlot.Q, 925).SetSkillshot(0.3f, 50, 1700, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(SpellSlot.Q, 925).SetSkillshot(0.3f, 50, 1700, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 1650).SetSkillshot(0, 90, 1750, false, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 290);
            R = new Spell(SpellSlot.R, 625);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.VeryHigh;

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Separator("Q/E: Always On");
                comboMenu.Separator("Sub Settings");
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Item", "Use Item");
                comboMenu.Separator("Swap Settings");
                comboMenu.Bool("SwapIfKill", "Swap W/R If Mark Can Kill Target", false);
                comboMenu.Slider("SwapIfHpU", "Swap W/R If Hp < (%)", 10);
                comboMenu.List("SwapGap", "Swap W/R To Gap Close", new[] { "OFF", "Smart", "Always" }, 1);
                comboMenu.Separator("W Settings");
                comboMenu.Bool("WNormal", "Use For Non-R Combo");
                comboMenu.List("WAdv", "Use For R Combo", new[] { "OFF", "Line", "Triangle", "Mouse" }, 1);
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Slider(
                    "RStopWRange",
                    "Dont Use W If R Ready And Distance <=",
                    (int)(R.Range + 200),
                    (int)R.Range,
                    (int)(R.Range + RangeW));
                comboMenu.Separator("Extra R Settings");
                GameObjects.EnemyHeroes.ForEach(
                    i => comboMenu.Bool("RCast" + i.ChampionName, "Cast On " + i.ChampionName, false));
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.List("Mode", "Mode", new[] { "W-E-Q", "E-Q", "Q" });
                hybridMenu.Separator("Auto Q Settings");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Slider("AutoQMpA", "If Mp >=", 100, 0, 200);
            }
            var lhMenu = MainMenu.Add(new Menu("LastHit", "Last Hit"));
            {
                lhMenu.Bool("Q", "Use Q");
            }
            var ksMenu = MainMenu.Add(new Menu("KillSteal", "Kill Steal"));
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("E", "Use E");
            }
            if (GameObjects.EnemyHeroes.Any())
            {
                Evade.Init();
                EvadeTarget.Init();
            }
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("W", "W Range", false);
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
                drawMenu.Bool("Target", "Target");
                drawMenu.Bool("DMark", "Death Mark");
                drawMenu.Bool("WPos", "W Shadow");
                drawMenu.Bool("RPos", "R Shadow");
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Evade.Evading += Evading;
            Evade.TryEvading += TryEvading;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe || args.SData.Name != "ZedW")
                    {
                        return;
                    }
                    var posStart = args.Start;
                    var posEnd = posStart.Extend(args.End, Math.Max(posStart.Distance(args.End), 350));
                    lastW = Variables.TickCount + (int)(1000 * posStart.Distance(posEnd) / W.Speed) - 100;
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!args.Buff.Caster.IsMe || sender.CharData.BaseSkinName != ShadowName || sender.IsEnemy)
                    {
                        return;
                    }
                    var shadow = sender as Obj_AI_Minion;
                    if (shadow.IsValid())
                    {
                        if (args.Buff.Name == "zedwshadowbuff")
                        {
                            wShadow = shadow;
                            lastW = 0;
                        }
                        else if (args.Buff.Name == "zedrshadowbuff")
                        {
                            rShadow = shadow;
                        }
                    }
                };
            Obj_AI_Base.OnPlayAnimation += (sender, args) =>
                {
                    if (args.Animation != "Death")
                    {
                        return;
                    }
                    var shadow = sender as Obj_AI_Minion;
                    if (shadow.IsValid())
                    {
                        if (wShadow.Compare(sender))
                        {
                            wShadow = null;
                        }
                        else if (rShadow.Compare(sender))
                        {
                            rShadow = null;
                        }
                    }
                };
            GameObject.OnCreate += (sender, args) =>
                {
                    if (sender.Name == RMarkName && rMarkObject == null)
                    {
                        rMarkObject = sender;
                    }
                };
            GameObject.OnDelete += (sender, args) =>
                {
                    if (sender.Compare(rMarkObject))
                    {
                        rMarkObject = null;
                    }
                    var shadow = sender as Obj_AI_Minion;
                    if (shadow.IsValid())
                    {
                        if (wShadow.Compare(shadow))
                        {
                            wShadow = null;
                        }
                        else if (rShadow.Compare(shadow))
                        {
                            rShadow = null;
                        }
                    }
                };
        }

        #endregion

        #region Properties

        private static Obj_AI_Hero GetTarget
        {
            get
            {
                if (RState == 0 && MainMenu["Combo"]["R"] && Variables.Orbwalker.GetActiveMode() == OrbwalkingMode.Combo)
                {
                    var targetR =
                        Variables.TargetSelector.GetTargets(Q.Range + RangeTarget, Q.DamageType)
                            .FirstOrDefault(i => MainMenu["Combo"]["RCast" + i.ChampionName]);
                    if (targetR != null)
                    {
                        return targetR;
                    }
                }
                var targets = Variables.TargetSelector.GetTargets(Q.Range + RangeTarget, Q.DamageType, false);
                if (targets.Count == 0)
                {
                    return null;
                }
                var target = targets.FirstOrDefault(HaveRMark);
                return target != null
                           ? (IsKillByMark(target)
                                  ? (targets.FirstOrDefault(i => !i.Compare(target)) ?? target)
                                  : target)
                           : targets.FirstOrDefault();
            }
        }

        private static bool IsRecentW => lastW > 0 && Variables.TickCount < lastW;

        private static float RangeTarget
        {
            get
            {
                var range = Q.Width / 2;
                if (wShadow.IsValid() && rShadow.IsValid())
                {
                    range += Math.Max(Player.Distance(rShadow), Player.Distance(wShadow));
                }
                else if (WState == 0 && rShadow.IsValid())
                {
                    range += Math.Max(Player.Distance(rShadow), RangeW);
                }
                else if (wShadow.IsValid())
                {
                    range += Player.Distance(wShadow);
                }
                else if (WState == 0)
                {
                    range += RangeW;
                }
                return range;
            }
        }

        private static int RState
            => R.IsReady() ? (R.Instance.Name == "ZedR" ? 0 : 1) : (R.Instance.Name == "ZedR" ? -1 : 2);

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
            var target = Q.GetTarget(Q.Width / 2);
            if (target == null)
            {
                return;
            }
            var pred = Q.VPrediction(target, true, CollisionableObjects.YasuoWall);
            if (pred.Hitchance >= Q.MinHitChance)
            {
                Q.Cast(pred.CastPosition);
            }
        }

        private static void CastE()
        {
            if (!E.IsReady() || IsRecentW)
            {
                return;
            }
            if (Variables.TargetSelector.GetTargets(float.MaxValue, E.DamageType, false).Any(IsInRangeE))
            {
                E.Cast();
            }
        }

        private static void CastQ(Obj_AI_Hero target)
        {
            if (!Q.IsReady() || IsRecentW)
            {
                return;
            }
            Q2.UpdateSourcePosition();
            var pPred = Q2.VPrediction(target, true, CollisionableObjects.YasuoWall);
            Prediction.PredictionOutput wPred = null;
            if (wShadow.IsValid())
            {
                Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                wPred = Q2.VPrediction(target, true, CollisionableObjects.YasuoWall);
            }
            Prediction.PredictionOutput rPred = null;
            if (rShadow.IsValid())
            {
                Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                rPred = Q2.VPrediction(target, true, CollisionableObjects.YasuoWall);
            }
            var pHitChance = pPred.Hitchance;
            var wHitChance = wPred?.Hitchance ?? HitChance.None;
            var rHitChance = rPred?.Hitchance ?? HitChance.None;
            var maxHit = (HitChance)Math.Max(Math.Max((int)wHitChance, (int)rHitChance), (int)pHitChance);
            if (maxHit >= Q2.MinHitChance)
            {
                if (maxHit == wHitChance && wPred != null)
                {
                    Q.Cast(wPred.CastPosition);
                }
                else if (maxHit == rHitChance && rPred != null)
                {
                    Q.Cast(rPred.CastPosition);
                }
                else if (maxHit == pHitChance)
                {
                    Q.Cast(pPred.CastPosition);
                }
            }
        }

        private static void CastW(Obj_AI_Hero target, bool isCombo = false)
        {
            if (WState != 0 || Variables.TickCount - W.LastCastAttemptT <= 1000)
            {
                return;
            }
            var posPred = W.VPrediction(target, true).CastPosition.ToVector2();
            var posPlayer = Player.ServerPosition.ToVector2();
            var posCast = posPlayer.Extend(posPred, RangeW);
            if (isCombo)
            {
                var posShadowR = rShadow.ServerPosition.ToVector2();
                switch (MainMenu["Combo"]["WAdv"].GetValue<MenuList>().Index)
                {
                    case 1:
                        posCast = posPlayer + (posPred - posShadowR).Normalized() * RangeW;
                        break;
                    case 2:
                        var subPos1 = posPlayer + (posPred - posShadowR).Normalized().Perpendicular() * RangeW;
                        var subPos2 = posPlayer + (posShadowR - posPred).Normalized().Perpendicular() * RangeW;
                        if (!subPos1.IsWall() && subPos2.IsWall())
                        {
                            posCast = subPos1;
                        }
                        else if (subPos1.IsWall() && !subPos2.IsWall())
                        {
                            posCast = subPos2;
                        }
                        else
                        {
                            posCast = subPos1.CountEnemyHeroesInRange(500) > subPos2.CountEnemyHeroesInRange(500)
                                          ? subPos1
                                          : subPos2;
                        }
                        break;
                    case 3:
                        posCast = Game.CursorPos.ToVector2();
                        break;
                }
            }
            W.Cast(posCast);
        }

        private static void Combo()
        {
            var target = GetTarget;
            if (target != null)
            {
                Swap(target);
                if (RState == 0 && MainMenu["Combo"]["R"] && MainMenu["Combo"]["RCast" + target.ChampionName]
                    && ((Q.IsReady(300) && Player.Mana >= Q.Instance.ManaCost - 10)
                        || (E.IsReady(300) && Player.Mana >= E.Instance.ManaCost - 10)))
                {
                    R.CastOnUnit(target);
                }
                if (MainMenu["Combo"]["Ignite"] && Ignite.IsReady() && (HaveRMark(target) || target.HealthPercent < 30)
                    && Player.Distance(target) < IgniteRange)
                {
                    Player.Spellbook.CastSpell(Ignite, target);
                }
                if (WState == 0
                    && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost
                         && Player.Distance(target) < RangeW + Q.Range)
                        || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                            && Player.Distance(target) < RangeW + E.Range)))
                {
                    if (MainMenu["Combo"]["WNormal"])
                    {
                        if (RState < 1
                            && (!MainMenu["Combo"]["R"] || !MainMenu["Combo"]["RCast" + target.ChampionName]
                                || (RState == 0 && Player.Distance(target) > MainMenu["Combo"]["RStopWRange"])
                                || RState == -1))
                        {
                            CastW(target);
                        }
                        if (rShadow.IsValid() && MainMenu["Combo"]["R"]
                            && MainMenu["Combo"]["RCast" + target.ChampionName] && !HaveRMark(target))
                        {
                            CastW(target);
                        }
                    }
                    if (MainMenu["Combo"]["WAdv"].GetValue<MenuList>().Index > 0 && rShadow.IsValid()
                        && MainMenu["Combo"]["R"] && MainMenu["Combo"]["RCast" + target.ChampionName]
                        && HaveRMark(target))
                    {
                        CastW(target, true);
                    }
                }
                CastE();
                CastQ(target);
            }
            if (MainMenu["Combo"]["Item"])
            {
                UseItem(target);
            }
        }

        private static void Evading(Obj_AI_Base sender)
        {
            var skillshot = Evade.SkillshotAboutToHit(sender, 50).OrderByDescending(i => i.DangerLevel);
            var zedW2 = EvadeSpellDatabase.Spells.FirstOrDefault(i => i.Enable && i.IsReady && i.Slot == SpellSlot.W);
            if (zedW2 != null && wShadow.IsValid()
                && (!wShadow.IsUnderEnemyTurret() || MainMenu["Evade"]["Spells"][zedW2.Name]["WTower"])
                && skillshot.Any(i => i.DangerLevel >= zedW2.DangerLevel))
            {
                sender.Spellbook.CastSpell(zedW2.Slot);
                return;
            }
            var zedR2 =
                EvadeSpellDatabase.Spells.FirstOrDefault(
                    i => i.Enable && i.IsReady && i.Slot == SpellSlot.R && i.CheckSpellName == "zedr2");
            if (zedR2 != null && rShadow.IsValid()
                && (!rShadow.IsUnderEnemyTurret() || MainMenu["Evade"]["Spells"][zedR2.Name]["RTower"])
                && skillshot.Any(i => i.DangerLevel >= zedR2.DangerLevel))
            {
                sender.Spellbook.CastSpell(zedR2.Slot);
            }
        }

        private static List<double> GetComboDmg(Obj_AI_Hero target, bool useQ, bool useW, bool useE, bool useR)
        {
            var dmgTotal = 0d;
            var manaTotal = 0f;
            if (MainMenu["Combo"]["Item"])
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
            dmgTotal += Player.GetAutoAttackDamage(target);
            if (useR || HaveRMark(target))
            {
                dmgTotal += Player.CalculateDamage(
                    target,
                    DamageType.Physical,
                    new[] { 0.3, 0.4, 0.5 }[R.Level - 1] * dmgTotal + Player.TotalAttackDamage);
            }
            return new List<double> { dmgTotal, manaTotal };
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
                     && Player.Distance(target) < RangeW + Q.Range)
                    || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                        && Player.Distance(target) < RangeW + E.Range)))
            {
                CastW(target);
            }
            if (MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index < 2)
            {
                CastE();
            }
            CastQ(target);
        }

        private static bool IsInRangeE(Obj_AI_Hero target)
        {
            var distPlayer = Player.Distance(target);
            var distW = wShadow.IsValid() ? wShadow.Distance(target) : float.MaxValue;
            var distR = rShadow.IsValid() ? rShadow.Distance(target) : float.MaxValue;
            return Math.Min(Math.Min(distR, distW), distPlayer) < E.Range;
        }

        private static bool IsInRangeQ(Obj_AI_Hero target)
        {
            var distPlayer = Player.Distance(target);
            var distW = wShadow.IsValid() ? wShadow.Distance(target) : float.MaxValue;
            var distR = rShadow.IsValid() ? rShadow.Distance(target) : float.MaxValue;
            return Math.Min(Math.Min(distR, distW), distPlayer) < Q.Range + Q.Width / 2;
        }

        private static bool IsKillByMark(Obj_AI_Hero target)
        {
            return HaveRMark(target) && rMarkObject != null && target.Distance(rMarkObject) < target.BoundingRadius
                   && !Invulnerable.Check(target, DamageType.True, false);
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                var targets =
                    Variables.TargetSelector.GetTargets(float.MaxValue, Q.DamageType)
                        .Where(i => !IsKillByMark(i) && IsInRangeQ(i))
                        .ToList();
                if (targets.Count > 0)
                {
                    var spellQ = new Spell(SpellSlot.Q, Q.Range).SetSkillshot(
                        Q.Delay,
                        Q.Width,
                        Q.Speed,
                        true,
                        SkillshotType.SkillshotLine);
                    foreach (var target in targets)
                    {
                        var dmgQ1 = Player.GetSpellDamage(target, SpellSlot.Q);
                        var dmgQ2 = Player.GetSpellDamage(target, SpellSlot.Q, Damage.DamageStage.SecondForm);
                        var totalHp = target.Health + target.PhysicalShield;
                        spellQ.UpdateSourcePosition();
                        var pPred = spellQ.VPrediction(target, true, QColObjects);
                        Prediction.PredictionOutput wPred = null;
                        if (wShadow.IsValid())
                        {
                            spellQ.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                            wPred = spellQ.VPrediction(target, true, QColObjects);
                        }
                        Prediction.PredictionOutput rPred = null;
                        if (rShadow.IsValid())
                        {
                            spellQ.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                            rPred = spellQ.VPrediction(target, true, QColObjects);
                        }
                        var pHitChance = pPred.Hitchance;
                        var wHitChance = wPred?.Hitchance ?? HitChance.None;
                        var rHitChance = rPred?.Hitchance ?? HitChance.None;
                        var maxHit = (HitChance)Math.Max(Math.Max((int)wHitChance, (int)rHitChance), (int)pHitChance);
                        if (maxHit >= Q2.MinHitChance && totalHp < dmgQ1)
                        {
                            if (maxHit == wHitChance && wPred != null)
                            {
                                Q.Cast(wPred.CastPosition);
                            }
                            else if (maxHit == rHitChance && rPred != null)
                            {
                                Q.Cast(rPred.CastPosition);
                            }
                            else if (maxHit == pHitChance)
                            {
                                Q.Cast(pPred.CastPosition);
                            }
                        }
                        else if (maxHit == HitChance.Collision && totalHp < dmgQ2)
                        {
                            if (maxHit == wHitChance && wPred != null && !wPred.CollisionObjects.All(i => i.IsMe))
                            {
                                Q.Cast(wPred.CastPosition);
                            }
                            else if (maxHit == rHitChance && rPred != null && !rPred.CollisionObjects.All(i => i.IsMe))
                            {
                                Q.Cast(rPred.CastPosition);
                            }
                            else if (maxHit == pHitChance && !pPred.CollisionObjects.All(i => i.IsMe))
                            {
                                Q.Cast(pPred.CastPosition);
                            }
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady()
                && Variables.TargetSelector.GetTargets(float.MaxValue, E.DamageType)
                       .Where(i => !IsKillByMark(i) && IsInRangeE(i))
                       .Any(i => i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.E)))
            {
                E.Cast();
            }
        }

        private static void LastHit()
        {
            if (!MainMenu["LastHit"]["Q"] || !Q.IsReady())
            {
                return;
            }
            foreach (var minion in
                GameObjects.EnemyMinions.Where(
                    i =>
                    i.IsValidTarget(Q.Range) && (i.IsMinion() || i.IsPet(false))
                    && (!i.InAutoAttackRange() ? Q.GetHealthPrediction(i) > 0 : i.Health > Player.GetAutoAttackDamage(i)))
                    .OrderByDescending(i => i.MaxHealth))
            {
                var pred = Q.VPrediction(minion, true, QColObjects);
                if (pred.Hitchance >= Q.MinHitChance
                    && Q.GetHealthPrediction(minion) <= Player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    Q.Cast(pred.CastPosition);
                }
                if (pred.Hitchance == HitChance.Collision && !pred.CollisionObjects.All(i => i.IsMe)
                    && Q.GetHealthPrediction(minion)
                    <= Player.GetSpellDamage(minion, SpellSlot.Q, Damage.DamageStage.SecondForm))
                {
                    Q.Cast(pred.CastPosition);
                }
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
                Drawing.DrawCircle(Player.Position, RangeW, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
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
                    Drawing.DrawCircle(target.Position, target.BoundingRadius * 1.5f, Color.Aqua);
                }
            }
            if (MainMenu["Draw"]["DMark"] && rMarkObject != null)
            {
                var target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsVisible && IsKillByMark(i));
                if (target != null)
                {
                    var pos = Drawing.WorldToScreen(Player.Position);
                    var text = "Death Mark: " + target.ChampionName;
                    Drawing.DrawText(pos.X - (float)Drawing.GetTextExtent(text).Width / 2, pos.Y + 20, Color.Red, text);
                }
            }
            if (MainMenu["Draw"]["WPos"] && wShadow.IsValid())
            {
                Drawing.DrawCircle(wShadow.Position, 100, Color.MediumSlateBlue);
                var pos = Drawing.WorldToScreen(wShadow.Position);
                Drawing.DrawText(pos.X - (float)Drawing.GetTextExtent("W").Width / 2, pos.Y, Color.BlueViolet, "W");
            }
            if (MainMenu["Draw"]["RPos"] && rShadow.IsValid())
            {
                Drawing.DrawCircle(rShadow.Position, 100, Color.MediumSlateBlue);
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
            switch (Variables.Orbwalker.GetActiveMode())
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Hybrid:
                    Hybrid();
                    break;
                case OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkingMode.None:
                    if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        if (WState == 0)
                        {
                            W.Cast(Game.CursorPos);
                        }
                        else if (WState == 1)
                        {
                            W.Cast();
                        }
                    }
                    break;
            }
            if (Variables.Orbwalker.GetActiveMode() < OrbwalkingMode.Combo
                || Variables.Orbwalker.GetActiveMode() > OrbwalkingMode.Hybrid)
            {
                AutoQ();
            }
        }

        //Credit: EB
        private static void Swap(Obj_AI_Hero target)
        {
            if (IsKillByMark(target))
            {
                return;
            }
            if (MainMenu["Combo"]["SwapGap"].GetValue<MenuList>().Index > 0 && !E.IsInRange(target))
            {
                var playerDist = Player.Distance(target);
                var wDist = WState == 1 && wShadow.IsValid() ? wShadow.Distance(target) : float.MaxValue;
                var rDist = RState == 1 && rShadow.IsValid() ? rShadow.Distance(target) : float.MaxValue;
                var minDist = Math.Min(Math.Min(wDist, rDist), playerDist);
                if (minDist < playerDist)
                {
                    switch (MainMenu["Combo"]["SwapGap"].GetValue<MenuList>().Index)
                    {
                        case 1:
                            if (Math.Abs(minDist - wDist) < float.Epsilon)
                            {
                                var calcCombo = GetComboDmg(
                                    target,
                                    Q.IsReady(),
                                    minDist < Q.Range || (rShadow.IsValid() && rShadow.Distance(target) < Q.Range),
                                    E.IsReady(),
                                    MainMenu["Combo"]["R"] && MainMenu["Combo"]["RCast" + target.ChampionName]
                                    && RState == 0 && minDist < R.Range);
                                if (target.Health + target.PhysicalShield < calcCombo[0] && Player.Mana >= calcCombo[1])
                                {
                                    W.Cast();
                                }
                                if (MainMenu["Combo"]["R"] && MainMenu["Combo"]["RCast" + target.ChampionName]
                                    && RState == 0 && !R.IsInRange(target) && minDist < R.Range
                                    && (Q.IsReady() || E.IsReady()))
                                {
                                    W.Cast();
                                }
                            }
                            else if (Math.Abs(minDist - rDist) < float.Epsilon)
                            {
                                var calcCombo = GetComboDmg(
                                    target,
                                    Q.IsReady(),
                                    minDist < Q.Range || (wShadow.IsValid() && wShadow.Distance(target) < Q.Range),
                                    E.IsReady(),
                                    false);
                                if (target.Health + target.PhysicalShield < calcCombo[0] && Player.Mana >= calcCombo[1])
                                {
                                    R.Cast();
                                }
                            }
                            break;
                        case 2:
                            if (minDist <= 500)
                            {
                                if (Math.Abs(minDist - wDist) < float.Epsilon)
                                {
                                    W.Cast();
                                }
                                else if (Math.Abs(minDist - rDist) < float.Epsilon)
                                {
                                    R.Cast();
                                }
                            }
                            break;
                    }
                }
            }
            if ((Player.HealthPercent < MainMenu["Combo"]["SwapIfHpU"] && Player.HealthPercent < target.HealthPercent)
                || (MainMenu["Combo"]["SwapIfKill"] && rMarkObject != null))
            {
                var wCount = WState == 1 && wShadow.IsValid() ? wShadow.CountEnemyHeroesInRange(400) : int.MaxValue;
                var rCount = RState == 1 && rShadow.IsValid() ? rShadow.CountEnemyHeroesInRange(400) : int.MaxValue;
                var minCount = Math.Min(rCount, wCount);
                if (Player.CountEnemyHeroesInRange(500) > minCount)
                {
                    if (minCount == wCount)
                    {
                        W.Cast();
                    }
                    else if (minCount == rCount)
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static void TryEvading(List<Skillshot> hitBy, Vector2 to)
        {
            var dangerLevel = hitBy.Select(i => i.DangerLevel).Concat(new[] { 0 }).Max();
            var zedR1 =
                EvadeSpellDatabase.Spells.FirstOrDefault(
                    i =>
                    i.Enable && dangerLevel >= i.DangerLevel && i.IsReady && i.Slot == SpellSlot.R
                    && i.CheckSpellName == "zedr");
            if (zedR1 == null)
            {
                return;
            }
            var target =
                Evader.GetEvadeTargets(zedR1.ValidTargets, int.MaxValue, zedR1.Delay, zedR1.MaxRange, true, false, true)
                    .OrderBy(i => i.CountEnemyHeroesInRange(RangeW))
                    .ThenByDescending(i => new Priority().GetDefaultPriority((Obj_AI_Hero)i))
                    .FirstOrDefault();
            if (target != null)
            {
                Player.Spellbook.CastSpell(zedR1.Slot, target);
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
            if (Youmuu.IsReady && Player.CountEnemyHeroesInRange(R.Range + E.Range) > 0)
            {
                Youmuu.Cast();
            }
            if (Tiamat.IsReady && Player.CountEnemyHeroesInRange(Tiamat.Range) > 0)
            {
                Tiamat.Cast();
            }
            if (Hydra.IsReady && Player.CountEnemyHeroesInRange(Hydra.Range) > 0)
            {
                Hydra.Cast();
            }
            if (Titanic.IsReady && Player.CountEnemyHeroesInRange(Titanic.Range) > 0)
            {
                Titanic.Cast();
            }
        }

        #endregion

        private static class EvadeTarget
        {
            #region Static Fields

            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            private static readonly List<SpellData> Spells = new List<SpellData>();

            #endregion

            #region Methods

            internal static void Init()
            {
                LoadSpellData();
                var evadeMenu = MainMenu.Add(new Menu("EvadeTarget", "Evade Target"));
                {
                    evadeMenu.Bool("R", "Use R1");
                    foreach (var hero in
                        GameObjects.EnemyHeroes.Where(
                            i =>
                            Spells.Any(
                                a =>
                                string.Equals(
                                    a.ChampionName,
                                    i.ChampionName,
                                    StringComparison.InvariantCultureIgnoreCase))))
                    {
                        evadeMenu.Add(new Menu(hero.ChampionName.ToLowerInvariant(), "-> " + hero.ChampionName));
                    }
                    foreach (var spell in
                        Spells.Where(
                            i =>
                            GameObjects.EnemyHeroes.Any(
                                a =>
                                string.Equals(
                                    a.ChampionName,
                                    i.ChampionName,
                                    StringComparison.InvariantCultureIgnoreCase))))
                    {
                        ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Bool(
                            spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            false);
                    }
                }
                Game.OnUpdate += OnUpdateTarget;
                GameObject.OnCreate += ObjSpellMissileOnCreate;
                GameObject.OnDelete += ObjSpellMissileOnDelete;
            }

            private static void LoadSpellData()
            {
                Spells.Add(
                    new SpellData { ChampionName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Brand", SpellNames = new[] { "brandwildfire", "brandwildfiremissile" },
                            Slot = SpellSlot.R
                        });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Caitlyn", SpellNames = new[] { "caitlynaceintheholemissile" },
                            Slot = SpellSlot.R
                        });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Leblanc", SpellNames = new[] { "leblancchaosorb", "leblancchaosorbm" },
                            Slot = SpellSlot.Q
                        });
                Spells.Add(new SpellData { ChampionName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrar" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Veigar", SpellNames = new[] { "veigarprimordialburst" }, Slot = SpellSlot.R });
            }

            private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }
                var caster = missile.SpellCaster as Obj_AI_Hero;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team || !missile.Target.IsMe)
                {
                    return;
                }
                var spellData =
                    Spells.FirstOrDefault(
                        i =>
                        i.SpellNames.Contains(missile.SData.Name.ToLower())
                        && MainMenu["EvadeTarget"][i.ChampionName.ToLowerInvariant()][i.MissileName]);
                if (spellData == null)
                {
                    return;
                }
                DetectedTargets.Add(new Targets { Obj = missile });
            }

            private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }
                var caster = missile.SpellCaster as Obj_AI_Hero;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team)
                {
                    return;
                }
                DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);
            }

            private static void OnUpdateTarget(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
                {
                    return;
                }
                if (!MainMenu["EvadeTarget"]["R"] || RState != 0)
                {
                    return;
                }
                if (DetectedTargets.Any(i => Player.Distance(i.Obj) < 500))
                {
                    var target = R.GetTarget();
                    if (target != null)
                    {
                        R.CastOnUnit(target);
                    }
                }
            }

            #endregion

            private class SpellData
            {
                #region Fields

                public string ChampionName;

                public SpellSlot Slot;

                public string[] SpellNames = { };

                #endregion

                #region Public Properties

                public string MissileName => this.SpellNames.First();

                #endregion
            }

            private class Targets
            {
                #region Fields

                public MissileClient Obj;

                #endregion
            }
        }
    }
}