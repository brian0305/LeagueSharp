namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.TSModes;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.UI.Menu;

    #endregion

    internal class Zed : Program
    {
        #region Static Fields

        private static GameObject deathMark;

        private static int lastW;

        private static bool wCasted, rCasted;

        private static MissileClient wMissile;

        private static Obj_AI_Minion wShadow, rShadow;

        private static int wShadowT, rShadowT;

        #endregion

        #region Constructors and Destructors

        public Zed()
        {
            Q = new Spell(SpellSlot.Q, 925).SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(Q.Slot, Q.Range).SetSkillshot(Q.Delay, Q.Width, Q.Speed, true, Q.Type);
            Q3 = new Spell(Q.Slot, Q.Range).SetSkillshot(Q.Delay, Q.Width, Q.Speed, true, Q.Type);
            W = new Spell(SpellSlot.W, 700).SetSkillshot(0, 0, 1750, false, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 290).SetTargetted(0.005f, float.MaxValue);
            R = new Spell(SpellSlot.R, 625);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            Q.MinHitChance = HitChance.VeryHigh;

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Separator("Q/E: Always On");
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
                comboMenu.KeyBind("R", "Use R", Keys.X, KeyBindType.Toggle);
                comboMenu.List("RMode", "Mode", new[] { "Always", "Wait Q/E" });
                comboMenu.Slider(
                    "RStopRange",
                    "Prevent Q/W/E If R Ready And Distance <",
                    (int)(R.Range + 200),
                    (int)R.Range,
                    (int)(R.Range + W.Range));
                if (GameObjects.EnemyHeroes.Any())
                {
                    comboMenu.Separator("Extra R Settings");
                    GameObjects.EnemyHeroes.ForEach(
                        i => comboMenu.Bool("RCast" + i.ChampionName, "Cast On " + i.ChampionName, false));
                }
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.List("Mode", "Mode", new[] { "W-E-Q", "E-Q", "Q" }, 1);
                hybridMenu.KeyBind("WEQ", "Only W If Hit E-Q", Keys.XButton2, KeyBindType.Toggle);
                hybridMenu.Separator("Auto Q Settings (Champ)");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Slider("AutoQMpA", "If Mp >=", 100, 0, 200);
                hybridMenu.Separator("Auto E Settings (Champ/Shadow)");
                hybridMenu.Bool("AutoE", "Auto", false);
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
                drawMenu.Bool("RStop", "Prevent Q/W/E Range", false);
                drawMenu.Bool("UseR", "R In Combo Status");
                drawMenu.Bool("Target", "Target");
                drawMenu.Bool("DMark", "Death Mark");
                drawMenu.Bool("WPos", "W Shadow");
                drawMenu.Bool("RPos", "R Shadow");
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
            Drawing.OnEndScene += OnEndScene;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    if (args.Slot == SpellSlot.W && args.SData.Name == "ZedW")
                    {
                        rCasted = false;
                        wCasted = true;
                        lastW = Variables.TickCount;
                    }
                    else if (args.Slot == SpellSlot.R && args.SData.Name == "ZedR")
                    {
                        wCasted = false;
                        rCasted = true;
                    }
                };
            GameObjectNotifier<Obj_AI_Minion>.OnCreate += (sender, minion) =>
                {
                    if (!minion.IsAlly || minion.CharData.BaseSkinName != "zedshadow")
                    {
                        return;
                    }
                    if (wCasted)
                    {
                        wShadowT = Variables.TickCount;
                        wShadow = minion;
                        wCasted = rCasted = false;
                    }
                    else if (rCasted)
                    {
                        rShadowT = Variables.TickCount;
                        rShadow = minion;
                        wCasted = rCasted = false;
                    }
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    var shadow = sender as Obj_AI_Minion;
                    if (shadow == null || !shadow.IsAlly || shadow.CharData.BaseSkinName != "zedshadow")
                    {
                        return;
                    }
                    switch (args.Buff.Name)
                    {
                        case "zedwshadowbuff":
                            if (!wShadow.Compare(shadow))
                            {
                                wShadowT = Variables.TickCount;
                                wShadow = shadow;
                            }
                            break;
                        case "zedrshadowbuff":
                            if (!rShadow.Compare(shadow))
                            {
                                rShadowT = Variables.TickCount;
                                rShadow = shadow;
                            }
                            break;
                    }
                };
            Obj_AI_Base.OnPlayAnimation += (sender, args) =>
                {
                    var shadow = sender as Obj_AI_Minion;
                    if (shadow == null || !shadow.IsAlly || args.Animation != "Death")
                    {
                        return;
                    }
                    if (shadow.Compare(wShadow))
                    {
                        wShadow = null;
                    }
                    else if (shadow.Compare(rShadow))
                    {
                        rShadow = null;
                    }
                };
            GameObjectNotifier<MissileClient>.OnCreate += (sender, args) =>
                {
                    var spellCaster = args.SpellCaster as Obj_AI_Hero;
                    if (spellCaster == null || !spellCaster.IsMe || args.SData.Name != "ZedWMissile")
                    {
                        return;
                    }
                    wMissile = args;
                };
            GameObjectNotifier<MissileClient>.OnDelete += (sender, args) =>
                {
                    if (!args.Compare(wMissile))
                    {
                        return;
                    }
                    wMissile = null;
                };
            GameObject.OnCreate += (sender, args) =>
                {
                    if (sender.Name != "Zed_Base_R_buf_tell.troy")
                    {
                        return;
                    }
                    var target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget() && HaveR(i));
                    if (target != null && target.Distance(sender) < 150)
                    {
                        deathMark = sender;
                    }
                };
            GameObject.OnDelete += (sender, args) =>
                {
                    if (sender.Compare(deathMark))
                    {
                        deathMark = null;
                    }
                };
        }

        #endregion

        #region Properties

        private static bool CanR
            =>
                MainMenu["Combo"]["RMode"].GetValue<MenuList>().Index == 0
                || (Q.IsReady(500) && Player.Mana >= Q.Instance.ManaCost - 10)
                || (E.IsReady(500) && Player.Mana >= E.Instance.ManaCost - 10);

        private static Obj_AI_Hero GetTarget
        {
            get
            {
                var extraRange = RangeTarget;
                if (Q.IsReady())
                {
                    extraRange += Q.Width / 2;
                }
                if (Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo
                    && MainMenu["Combo"]["R"].GetValue<MenuKeyBind>().Active && RState == 0)
                {
                    var targetR =
                        Variables.TargetSelector.GetTargets(Q.Range + extraRange, Q.DamageType)
                            .OrderByDescending(i => new Priority().GetPriority(i))
                            .ThenBy(i => i.DistanceToPlayer())
                            .FirstOrDefault(i => MainMenu["Combo"]["RCast" + i.ChampionName]);
                    if (targetR != null)
                    {
                        return targetR;
                    }
                }
                var targets = Variables.TargetSelector.GetTargets(Q.Range + extraRange, Q.DamageType);
                if (targets.Count == 0)
                {
                    return null;
                }
                var target = targets.FirstOrDefault(HaveR);
                return target != null
                           ? (IsKillByMark(target)
                                  ? (targets.FirstOrDefault(i => !i.Compare(target)) ?? target)
                                  : target)
                           : targets.FirstOrDefault();
            }
        }

        private static bool IsCastingW
            => !wShadow.IsValid() && wMissile != null && wMissile.Distance(wMissile.EndPosition) < 80;

        private static bool IsROne => R.Instance.SData.Name == "ZedR";

        private static bool IsWOne => W.Instance.SData.Name == "ZedW";

        private static float RangeTarget
        {
            get
            {
                var validW = wShadow.IsValid();
                var validR = rShadow.IsValid();
                var posW = validW ? wShadow.ServerPosition : new Vector3();
                if (!posW.IsValid() && wMissile != null)
                {
                    validW = true;
                    posW = wMissile.EndPosition;
                }
                return validW && validR
                           ? Math.Max(rShadow.DistanceToPlayer(), posW.DistanceToPlayer())
                           : (WState == 0 && Variables.TickCount - lastW > 150
                                  ? (validR ? Math.Max(rShadow.DistanceToPlayer(), W.Range) : W.Range)
                                  : (validW ? posW.DistanceToPlayer() : (validR ? rShadow.DistanceToPlayer() : 0)));
            }
        }

        private static bool RShadowCanQ
            => rShadow.IsValid() && Variables.TickCount - rShadowT <= 7500 - Q.Delay * 1000 + 50;

        private static int RState => R.IsReady() ? (IsROne ? 0 : 1) : (IsROne ? -1 : 2);

        private static bool WShadowCanQ
            => wShadow.IsValid() && Variables.TickCount - wShadowT <= 4500 - Q.Delay * 1000 + 50;

        private static int WState => W.IsReady() ? (IsWOne ? 0 : 1) : -1;

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!Q.IsReady() || !MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || Player.Mana < MainMenu["Hybrid"]["AutoQMpA"])
            {
                return;
            }
            Q.CastingBestTarget(true, CollisionableObjects.YasuoWall);
        }

        private static SpellSlot CanW(Obj_AI_Hero target)
        {
            if (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                && target.DistanceToPlayer() < W.Range + E.Range - 20)
            {
                return SpellSlot.E;
            }
            if (Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost
                && target.DistanceToPlayer() < W.Range + Q.Range)
            {
                return SpellSlot.Q;
            }
            return SpellSlot.Unknown;
        }

        private static void CastE(bool onlyKill = false)
        {
            if (!E.IsReady())
            {
                return;
            }
            var targets = Variables.TargetSelector.GetTargets(E.Range + 20 + RangeTarget, E.DamageType);
            if (onlyKill)
            {
                targets = targets.Where(i => !IsKillByMark(i) && i.Health + i.PhysicalShield <= E.GetDamage(i)).ToList();
            }
            if (targets.Count == 0)
            {
                return;
            }
            if (targets.Any(IsInRangeE))
            {
                E.Cast();
            }
        }

        private static void CastQ(Obj_AI_Hero target)
        {
            if (!Q.IsReady())
            {
                return;
            }
            var pred = Q.GetPrediction(target, true, -1, CollisionableObjects.YasuoWall);
            if (pred.Hitchance >= Q.MinHitChance)
            {
                Q.Cast(pred.CastPosition);
            }
            else
            {
                PredictionOutput predShadow = null;
                if (WShadowCanQ)
                {
                    Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                    predShadow = Q2.GetPrediction(target, true, -1, CollisionableObjects.YasuoWall);
                }
                else if (IsCastingW)
                {
                    Q2.UpdateSourcePosition(wMissile.EndPosition, wMissile.EndPosition);
                    predShadow = Q2.GetPrediction(target, true, -1, CollisionableObjects.YasuoWall);
                }
                if (predShadow != null && predShadow.Hitchance >= Q.MinHitChance)
                {
                    Q.Cast(predShadow.CastPosition);
                }
                else if (RShadowCanQ)
                {
                    Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                    predShadow = Q2.GetPrediction(target, true, -1, CollisionableObjects.YasuoWall);
                    if (predShadow.Hitchance >= Q.MinHitChance)
                    {
                        Q.Cast(predShadow.CastPosition);
                    }
                }
            }
        }

        private static bool CastQKill(Spell spell, Obj_AI_Base target)
        {
            var pred1 = spell.GetPrediction(target, false, -1, CollisionableObjects.YasuoWall);
            if (pred1.Hitchance < Q.MinHitChance)
            {
                return false;
            }
            var pred2 = spell.GetPrediction(
                target,
                false,
                -1,
                CollisionableObjects.Heroes | CollisionableObjects.Minions);
            if (pred2.Hitchance == HitChance.Collision)
            {
                switch (target.Type)
                {
                    case GameObjectType.obj_AI_Hero:
                        return target.Health + target.PhysicalShield <= Q.GetDamage(target, DamageStage.SecondForm)
                               && Q.Cast(pred1.CastPosition);
                    case GameObjectType.obj_AI_Minion:
                        return Q.CanLastHit(target, Q.GetDamage(target, DamageStage.SecondForm))
                               && Q.Cast(pred1.CastPosition);
                }
                return false;
            }
            return pred2.Hitchance >= Q.MinHitChance && Q.Cast(pred2.CastPosition);
        }

        private static void CastW(Obj_AI_Hero target, SpellSlot slot, bool isRCombo = false)
        {
            if (slot == SpellSlot.Unknown || Variables.TickCount - lastW <= 500 || wMissile != null)
            {
                return;
            }
            switch (slot)
            {
                case SpellSlot.Q:
                    W.Width = Q.Width + 30;
                    W.Delay = 0;
                    break;
                case SpellSlot.E:
                    W.Width = E.Width / 2;
                    W.Delay = E.Delay;
                    break;
            }
            var posCast = W.GetPrediction(target).UnitPosition;
            if (isRCombo)
            {
                var posEnd = rShadow.ServerPosition;
                if (posCast.Distance(posEnd) > Q.Range - 50)
                {
                    posEnd = Player.ServerPosition;
                }
                switch (MainMenu["Combo"]["WAdv"].GetValue<MenuList>().Index)
                {
                    case 1:
                        posCast = Player.ServerPosition + (posCast - posEnd).Normalized() * 550;
                        break;
                    case 2:
                        var subPos1 = Player.ServerPosition + (posCast - posEnd).Normalized().Perpendicular() * 550;
                        var subPos2 = Player.ServerPosition + (posEnd - posCast).Normalized().Perpendicular() * 550;
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
                            posCast = subPos1.CountEnemyHeroesInRange(350) > subPos2.CountEnemyHeroesInRange(350)
                                          ? subPos1
                                          : subPos2;
                        }
                        break;
                    case 3:
                        posCast = Game.CursorPos;
                        break;
                }
            }
            else if (posCast.DistanceToPlayer() < E.Range * 2 - target.BoundingRadius)
            {
                posCast = Player.ServerPosition.Extend(posCast, 500);
            }
            else if (posCast.DistanceToPlayer() > 550 && posCast.DistanceToPlayer() < 650)
            {
                posCast = Player.ServerPosition.Extend(posCast, 600);
            }
            W.Cast(posCast);
        }

        private static void Combo()
        {
            var target = GetTarget;
            if (target != null)
            {
                Swap(target);
                var useR = MainMenu["Combo"]["R"].GetValue<MenuKeyBind>().Active
                           && MainMenu["Combo"]["RCast" + target.ChampionName]
                           && target.Health + target.PhysicalShield > Q.GetDamage(target);
                if (RState == 0 && useR && R.IsInRange(target) && CanR && R.CastOnUnit(target))
                {
                    return;
                }
                if (MainMenu["Combo"]["Ignite"] && Common.CanIgnite && (HaveR(target) || target.HealthPercent < 25)
                    && target.DistanceToPlayer() < IgniteRange)
                {
                    Player.Spellbook.CastSpell(Ignite, target);
                }
                var canCast = !useR || (RState == 0 && target.DistanceToPlayer() >= MainMenu["Combo"]["RStopRange"])
                              || RState == -1;
                if (WState == 0)
                {
                    var slot = CanW(target);
                    if (slot != SpellSlot.Unknown)
                    {
                        if (MainMenu["Combo"]["WAdv"].GetValue<MenuList>().Index > 0 && rShadow.IsValid() && useR
                            && HaveR(target) && !IsKillByMark(target))
                        {
                            CastW(target, slot, true);
                            return;
                        }
                        if (MainMenu["Combo"]["WNormal"]
                            && ((RState < 1 && canCast) || (rShadow.IsValid() && useR && !HaveR(target))))
                        {
                            CastW(target, slot);
                            return;
                        }
                    }
                    else if (MainMenu["Combo"]["WNormal"] && Variables.TickCount - lastW > 500
                             && target.Health + target.PhysicalShield <= Player.GetAutoAttackDamage(target)
                             && !E.IsInRange(target) && !IsKillByMark(target)
                             && target.DistanceToPlayer() < W.Range + target.GetRealAutoAttackRange() - 100
                             && W.Cast(
                                 target.ServerPosition.Extend(Player.ServerPosition, -target.GetRealAutoAttackRange())))
                    {
                        return;
                    }
                }
                if (canCast || rShadow.IsValid())
                {
                    CastE();
                    CastQ(target);
                }
            }
            if (MainMenu["Combo"]["Item"])
            {
                UseItem(target);
            }
        }

        private static List<double> GetCombo(Obj_AI_Hero target, bool useQ, bool useW, bool useE)
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
                if (Tiamat.IsReady || Hydra.IsReady)
                {
                    dmgTotal += Player.CalculateDamage(target, DamageType.Physical, Player.TotalAttackDamage);
                }
                if (Titanic.IsReady)
                {
                    dmgTotal += Player.CalculateDamage(target, DamageType.Physical, 40 + 0.1f * Player.MaxHealth);
                }
            }
            if (useQ)
            {
                dmgTotal += Q.GetDamage(target);
                manaTotal += Q.Instance.ManaCost;
            }
            if (useW)
            {
                if (useQ)
                {
                    dmgTotal += Q.GetDamage(target) / 2;
                }
                if (WState == 0)
                {
                    manaTotal += W.Instance.ManaCost;
                }
            }
            if (useE)
            {
                dmgTotal += E.GetDamage(target);
                manaTotal += E.Instance.ManaCost;
            }
            dmgTotal += Player.GetAutoAttackDamage(target) * 2;
            if (HaveR(target))
            {
                dmgTotal += Player.CalculateDamage(
                    target,
                    DamageType.Physical,
                    new[] { 0.25, 0.35, 0.45 }[R.Level - 1] * dmgTotal + Player.TotalAttackDamage);
            }
            return new List<double> { dmgTotal, manaTotal };
        }

        private static bool HaveR(Obj_AI_Hero target)
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
            var canCast = true;
            var mode = MainMenu["Hybrid"]["Mode"].GetValue<MenuList>().Index;
            if (mode == 0 && WState == 0)
            {
                if (MainMenu["Hybrid"]["WEQ"].GetValue<MenuKeyBind>().Active)
                {
                    bool canQ = Q.Level > 0, canE = E.Level > 0;
                    if (canQ || canE)
                    {
                        canCast = (!canE || E.IsReady()) && (!canQ || Q.IsReady(100))
                                  && Player.Mana
                                  >= (canE ? E.Instance.ManaCost : 0) + ((canQ ? Q.Instance.ManaCost : 0) - 10)
                                  + W.Instance.ManaCost && Q.IsInRange(target);
                    }
                }
                if (canCast)
                {
                    CastW(target, MainMenu["Hybrid"]["WEQ"].GetValue<MenuKeyBind>().Active ? SpellSlot.E : CanW(target));
                    return;
                }
            }
            if (!canCast)
            {
                return;
            }
            if (mode < 2)
            {
                CastE();
            }
            CastQ(target);
        }

        private static bool IsInRangeE(Obj_AI_Hero target)
        {
            var pos = E.GetPredPosition(target);
            return pos.DistanceToPlayer() < E.Range || (wShadow.IsValid() && wShadow.Distance(pos) < E.Range)
                   || (rShadow.IsValid() && rShadow.Distance(pos) < E.Range)
                   || (IsCastingW && wMissile.EndPosition.Distance(pos) < E.Range);
        }

        private static bool IsKillByMark(Obj_AI_Hero target)
        {
            return HaveR(target) && deathMark != null && target.Distance(deathMark) < 150;
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                var targets =
                    Variables.TargetSelector.GetTargets(Q.Range + Q.Width / 2 + RangeTarget, Q.DamageType)
                        .Where(i => !IsKillByMark(i) && i.Health + i.PhysicalShield <= Q.GetDamage(i))
                        .ToList();
                if (targets.Count > 0)
                {
                    foreach (var target in targets)
                    {
                        if (CastQKill(Q, target))
                        {
                            return;
                        }
                        if (WShadowCanQ)
                        {
                            Q3.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                            if (CastQKill(Q3, target))
                            {
                                return;
                            }
                        }
                        else if (IsCastingW)
                        {
                            Q3.UpdateSourcePosition(wMissile.EndPosition, wMissile.EndPosition);
                            if (CastQKill(Q3, target))
                            {
                                return;
                            }
                        }
                        if (RShadowCanQ)
                        {
                            Q3.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                            CastQKill(Q3, target);
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady())
            {
                CastE(true);
            }
        }

        private static void LastHit()
        {
            if (!MainMenu["LastHit"]["Q"] || !Q.IsReady() || Player.Spellbook.IsAutoAttacking)
            {
                return;
            }
            var minions =
                GameObjects.EnemyMinions.Where(
                    i => (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range) && Q.CanLastHit(i, Q.GetDamage(i)))
                    .OrderByDescending(i => i.MaxHealth)
                    .ToList();
            if (minions.Count == 0)
            {
                return;
            }
            minions.ForEach(i => CastQKill(Q, i));
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (MainMenu["Draw"]["UseR"] && R.Level > 0)
            {
                var menu = MainMenu["Combo"]["R"].GetValue<MenuKeyBind>();
                var pos = Drawing.WorldToScreen(Player.Position);
                var text = $"Use R In Combo: {(menu.Active ? "On" : "Off")} [{menu.Key}]";
                Drawing.DrawText(
                    pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                    pos.Y + 20,
                    menu.Active ? Color.White : Color.Gray,
                    text);
            }
            if (MainMenu["Draw"]["DMark"] && rShadow.IsValid())
            {
                var target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget() && IsKillByMark(i));
                if (target != null)
                {
                    var pos = Drawing.WorldToScreen(Player.Position);
                    var text = "Death Mark: " + target.ChampionName;
                    Drawing.DrawText(pos.X - (float)Drawing.GetTextExtent(text).Width / 2, pos.Y + 40, Color.Red, text);
                }
            }
            if (MainMenu["Draw"]["WPos"] && wShadow.IsValid())
            {
                var pos = Drawing.WorldToScreen(wShadow.Position);
                Drawing.DrawText(pos.X - (float)Drawing.GetTextExtent("W").Width / 2, pos.Y, Color.BlueViolet, "W");
            }
            if (MainMenu["Draw"]["RPos"] && rShadow.IsValid())
            {
                var pos = Drawing.WorldToScreen(rShadow.Position);
                Drawing.DrawText(pos.X - (float)Drawing.GetTextExtent("R").Width / 2, pos.Y, Color.BlueViolet, "R");
            }
        }

        private static void OnEndScene(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (MainMenu["Draw"]["Q"] && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["W"] && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (R.Level > 0 && RState == 0)
            {
                if (MainMenu["Draw"]["R"])
                {
                    Render.Circle.DrawCircle(Player.Position, R.Range, Color.LimeGreen);
                }
                if (MainMenu["Draw"]["RStop"] && MainMenu["Combo"]["R"].GetValue<MenuKeyBind>().Active)
                {
                    Render.Circle.DrawCircle(Player.Position, MainMenu["Combo"]["RStopRange"], Color.Orange);
                }
            }
            if (MainMenu["Draw"]["Target"])
            {
                var target = GetTarget;
                if (target != null)
                {
                    Render.Circle.DrawCircle(target.Position, target.BoundingRadius * 1.5f, Color.Aqua);
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            switch (Variables.Orbwalker.ActiveMode)
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
            if (Variables.Orbwalker.ActiveMode != OrbwalkingMode.Combo)
            {
                if (MainMenu["Hybrid"]["AutoE"])
                {
                    CastE();
                }
                if (Variables.Orbwalker.ActiveMode != OrbwalkingMode.Hybrid)
                {
                    AutoQ();
                }
            }
        }

        private static void Swap(Obj_AI_Hero target)
        {
            var eCanKill = E.CanCast(target) && E.CanHitCircle(target)
                           && target.Health + target.PhysicalShield <= E.GetDamage(target);
            var markCanKill = IsKillByMark(target);
            if (MainMenu["Combo"]["SwapIfKill"] && (markCanKill || eCanKill))
            {
                SwapCountEnemy();
                return;
            }
            if (Player.HealthPercent < MainMenu["Combo"]["SwapIfHpU"])
            {
                if (markCanKill || !eCanKill || Player.HealthPercent < target.HealthPercent)
                {
                    SwapCountEnemy();
                }
            }
            else if (MainMenu["Combo"]["SwapGap"].GetValue<MenuList>().Index > 0 && !E.IsInRange(target) && !markCanKill)
            {
                var wDist = WState == 1 && wShadow.IsValid() ? wShadow.Distance(target) : float.MaxValue;
                var rDist = RState == 1 && rShadow.IsValid() ? rShadow.Distance(target) : float.MaxValue;
                var minDist = Math.Min(wDist, rDist);
                if (minDist.Equals(float.MaxValue) || target.DistanceToPlayer() <= minDist)
                {
                    return;
                }
                var swapByW = Math.Abs(minDist - wDist) < float.Epsilon;
                var swapByR = Math.Abs(minDist - rDist) < float.Epsilon;
                if (swapByW && minDist < R.Range && !R.IsInRange(target)
                    && MainMenu["Combo"]["R"].GetValue<MenuKeyBind>().Active
                    && MainMenu["Combo"]["RCast" + target.ChampionName] && RState == 0 && CanR && W.Cast())
                {
                    return;
                }
                switch (MainMenu["Combo"]["SwapGap"].GetValue<MenuList>().Index)
                {
                    case 1:
                        if (IsInRangeE(target) && target.HealthPercent < 15 && Player.HealthPercent > 30
                            && (Q.IsReady() || E.IsReady()))
                        {
                            if (swapByW)
                            {
                                W.Cast();
                            }
                            else if (swapByR)
                            {
                                R.Cast();
                            }
                            return;
                        }
                        var combo = GetCombo(
                            target,
                            Q.IsReady() && minDist < Q.Range,
                            false,
                            E.IsReady() && minDist < E.Range);
                        if (minDist > target.GetRealAutoAttackRange())
                        {
                            combo[0] -= Player.GetAutoAttackDamage(target);
                        }
                        if (minDist > target.GetRealAutoAttackRange() + 100)
                        {
                            combo[0] -= Player.GetAutoAttackDamage(target);
                        }
                        if (target.Health + target.PhysicalShield > combo[0] || Player.Mana < combo[1])
                        {
                            return;
                        }
                        if (swapByW)
                        {
                            W.Cast();
                        }
                        else if (swapByR)
                        {
                            R.Cast();
                        }
                        break;
                    case 2:
                        if (minDist > 500)
                        {
                            return;
                        }
                        if (swapByW)
                        {
                            W.Cast();
                        }
                        else if (swapByR)
                        {
                            R.Cast();
                        }
                        break;
                }
            }
        }

        private static void SwapCountEnemy()
        {
            var wCount = WState == 1 && wShadow.IsValid() ? wShadow.CountEnemyHeroesInRange(400) : int.MaxValue;
            var rCount = RState == 1 && rShadow.IsValid() ? rShadow.CountEnemyHeroesInRange(400) : int.MaxValue;
            var minCount = Math.Min(rCount, wCount);
            if (minCount == int.MaxValue || Player.CountEnemyHeroesInRange(400) <= minCount)
            {
                return;
            }
            if (minCount == wCount)
            {
                W.Cast();
            }
            else if (minCount == rCount)
            {
                R.Cast();
            }
        }

        private static void UseItem(Obj_AI_Hero target)
        {
            if (target != null && (HaveR(target) || target.HealthPercent < 40 || Player.HealthPercent < 50))
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
            if (Titanic.IsReady && !Player.Spellbook.IsAutoAttacking && Variables.Orbwalker.GetTarget() != null)
            {
                Titanic.Cast();
            }
        }

        #endregion

        private static class EvadeTarget
        {
            #region Static Fields

            private static readonly List<MissileClient> DetectedTargets = new List<MissileClient>();

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
                GameObjectNotifier<MissileClient>.OnCreate += EvadeTargetOnCreate;
                GameObjectNotifier<MissileClient>.OnDelete += EvadeTargetOnDelete;
            }

            private static void EvadeTargetOnCreate(object sender, MissileClient missile)
            {
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
                DetectedTargets.Add(missile);
            }

            private static void EvadeTargetOnDelete(object sender, MissileClient missile)
            {
                var caster = missile.SpellCaster as Obj_AI_Hero;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team)
                {
                    return;
                }
                DetectedTargets.RemoveAll(i => i.Compare(missile));
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
                            ChampionName = "FiddleSticks",
                            SpellNames = new[] { "fiddlesticksdarkwind", "fiddlesticksdarkwindmissile" },
                            Slot = SpellSlot.E
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
                    new SpellData { ChampionName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Veigar", SpellNames = new[] { "veigarprimordialburst" }, Slot = SpellSlot.R });
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
                if (!MainMenu["EvadeTarget"]["R"] || RState != 0 || DetectedTargets.Count == 0)
                {
                    return;
                }
                foreach (var missile in DetectedTargets.OrderBy(i => i.Distance(Player)))
                {
                    if (Player.Distance(missile) < 250)
                    {
                        var target =
                            GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(R.Range))
                                .MaxOrDefault(i => new Priority().GetDefaultPriority(i));
                        if (target != null)
                        {
                            R.CastOnUnit(target);
                        }
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
        }
    }
}