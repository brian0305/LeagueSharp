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

    internal class Zed : Program
    {
        #region Constants

        private const float OverkillValue = 1.1f;

        private const int RangeW = 750;

        #endregion

        #region Static Fields

        private static Obj_GeneralParticleEmitter deathMark;

        private static bool wCasted, rCasted;

        private static Obj_AI_Minion wShadow, rShadow;

        #endregion

        #region Constructors and Destructors

        public Zed()
        {
            Q = new Spell(SpellSlot.Q, 950).SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(SpellSlot.Q, 950).SetSkillshot(0.25f, 50, 1700, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 1700).SetSkillshot(0, 50, 1750, false, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 290);
            R = new Spell(SpellSlot.R, 625);
            Q.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            Q.MinHitChance = HitChance.VeryHigh;

            var orbwalkMenu = MainMenu.Add(new Menu("Orbwalk", "Orbwalk"));
            {
                orbwalkMenu.Separator("Q/E: Always On");
                orbwalkMenu.Separator("Sub Settings");
                orbwalkMenu.Bool("Ignite", "Use Ignite");
                orbwalkMenu.Bool("Item", "Use Item");
                orbwalkMenu.Separator("Swap Settings");
                orbwalkMenu.Bool("SwapIfKill", "Swap W/R If Mark Can Kill Target");
                orbwalkMenu.Slider("SwapIfHpU", "Swap W/R If Hp < (%)", 10);
                orbwalkMenu.List("SwapGap", "Swap W/R To Gap Close", new[] { "OFF", "Smart", "Always" }, 1);
                orbwalkMenu.Separator("W Settings");
                orbwalkMenu.Bool("WNormal", "Use For Non-R Combo");
                orbwalkMenu.List("WAdv", "Use For R Combo", new[] { "OFF", "Line", "Triangle", "Mouse" }, 1);
                orbwalkMenu.Separator("R Settings");
                orbwalkMenu.Bool("R", "Use R");
                orbwalkMenu.Slider(
                    "RStopRange",
                    "Priorize If Ready And Distance <=",
                    (int)(R.Range + 200),
                    (int)R.Range,
                    (int)(R.Range + RangeW));
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
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += (sender, args) =>
                {
                    var mark = sender as Obj_GeneralParticleEmitter;
                    if (rShadow.IsValid() && mark.Compare(deathMark))
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
                if (RState == 0 && MainMenu["Orbwalk"]["R"] && Orbwalker.ActiveMode == OrbwalkingMode.Combo)
                {
                    var target =
                        Variables.TargetSelector.GetTargets(Q.Range + RangeTarget, Q.DamageType)
                            .FirstOrDefault(i => MainMenu["Orbwalk"]["RCast" + i.ChampionName]);
                    if (target != null)
                    {
                        return target;
                    }
                }
                if (rShadow.IsValid())
                {
                    var target =
                        GameObjects.EnemyHeroes.FirstOrDefault(
                            i => i.IsValidTarget(Q.Range + RangeTarget) && HaveRMark(i));
                    if (target != null)
                    {
                        if (DeadByRMark(target))
                        {
                            var otherTarget = Q.GetTarget(RangeTarget, false, new List<Obj_AI_Hero> { target });
                            if (otherTarget != null)
                            {
                                return otherTarget;
                            }
                        }
                        return target;
                    }
                }
                return Q.GetTarget(RangeTarget);
            }
        }

        private static float RangeTarget
        {
            get
            {
                var range = Q.Width;
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
            =>
                R.IsReady()
                    ? (R.Instance.SData.Name.ToLower() != "zedr2" ? 0 : 1)
                    : (rShadow.IsValid() && R.Instance.SData.Name.ToLower() == "zedr2" ? 2 : -1);

        private static int WState => W.IsReady() ? (W.Instance.SData.Name.ToLower() != "zedw2" ? 0 : 1) : -1;

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!Q.IsReady() || !MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || Player.Mana < MainMenu["Hybrid"]["AutoQMpA"])
            {
                return;
            }
            var target = Q.GetTarget(Q.Width);
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

        private static void CastE(Obj_AI_Hero hero = null)
        {
            if (!E.IsReady())
            {
                return;
            }
            if (hero != null)
            {
                if (IsInRangeE(hero))
                {
                    E.Cast();
                }
            }
            else if (GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()).Any(IsInRangeE))
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
            var predPlayer = Q.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
            Prediction.PredictionOutput predW = null;
            if (wShadow.IsValid())
            {
                Q2.UpdateSourcePosition(wShadow.ServerPosition, wShadow.ServerPosition);
                predW = Q2.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
            }
            Prediction.PredictionOutput predR = null;
            if (rShadow.IsValid())
            {
                Q2.UpdateSourcePosition(rShadow.ServerPosition, rShadow.ServerPosition);
                predR = Q2.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
            }
            var bestHit =
                (HitChance)
                Math.Max(
                    Math.Max((int)(predW?.Hitchance ?? HitChance.None), (int)(predR?.Hitchance ?? HitChance.None)),
                    (int)predPlayer.Hitchance);
            if (bestHit < Q.MinHitChance)
            {
                return;
            }
            if (predW != null && bestHit == predW.Hitchance)
            {
                Q.Cast(predW.CastPosition);
            }
            else if (predR != null && bestHit == predR.Hitchance)
            {
                Q.Cast(predR.CastPosition);
            }
            else
            {
                Q.Cast(predPlayer.CastPosition);
            }
        }

        private static void CastW(Obj_AI_Hero target, bool isCombo = false)
        {
            if (WState != 0 || Variables.TickCount - W.LastCastAttemptT <= 1000)
            {
                return;
            }
            var posPred = W.VPrediction(target, true).UnitPosition;
            var posCast = Player.ServerPosition.Extend(posPred, RangeW);
            if (isCombo)
            {
                switch (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index)
                {
                    case 1:
                        posCast = Player.ServerPosition + (posPred - rShadow.ServerPosition).Normalized() * RangeW;
                        break;
                    case 2:
                        var subPos1 = Player.ServerPosition
                                      + (posPred - rShadow.ServerPosition).Normalized().Perpendicular() * RangeW;
                        var subPos2 = Player.ServerPosition
                                      + (rShadow.ServerPosition - posPred).Normalized().Perpendicular() * RangeW;
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
                            posCast = subPos1;
                        }
                        break;
                    case 3:
                        posCast = Game.CursorPos;
                        break;
                }
            }
            W.Cast(posCast);
        }

        private static bool DeadByRMark(Obj_AI_Hero target)
        {
            return rShadow.IsValid() && target != null && HaveRMark(target) && deathMark != null
                   && target.Distance(deathMark) < 50 + target.BoundingRadius;
        }

        private static void Farm()
        {
            if (!MainMenu["Farm"]["Q"] || !Q.IsReady())
            {
                return;
            }
            foreach (var minion in
                GameObjects.EnemyMinions.Where(
                    i =>
                    i.IsValidTarget(Q.Range) && i.IsMinion()
                    && (!i.InAutoAttackRange() ? Q.GetHealthPrediction(i) > 0 : i.Health > Player.GetAutoAttackDamage(i)))
                    .OrderByDescending(i => i.MaxHealth))
            {
                var pred = Q.VPrediction(
                    minion,
                    true,
                    new[] { CollisionableObjects.Heroes, CollisionableObjects.Minions, CollisionableObjects.YasuoWall });
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
            dmgTotal += Player.GetAutoAttackDamage(target) * 2;
            if (useR || HaveRMark(target))
            {
                dmgTotal += Player.CalculateDamage(
                    target,
                    DamageType.Physical,
                    new[] { 0.3, 0.4, 0.5 }[R.Level - 1] * dmgTotal + Player.TotalAttackDamage);
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
            var posPred = Prediction.GetPrediction(target, 0).UnitPosition;
            var distPlayer = Player.Distance(posPred);
            var distW = wShadow.IsValid() ? wShadow.Distance(posPred) : 999999;
            var distR = rShadow.IsValid() ? rShadow.Distance(posPred) : 999999;
            return Math.Min(Math.Min(distR, distW), distPlayer) < E.Range;
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                foreach (var hero in
                    GameObjects.EnemyHeroes.Where(
                        i => i.IsValidTarget() && i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.Q)))
                {
                    CastQ(hero);
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady())
            {
                foreach (var hero in
                    GameObjects.EnemyHeroes.Where(
                        i => i.IsValidTarget() && i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.E)))
                {
                    CastE(hero);
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var shadow = sender as Obj_AI_Minion;
            if (shadow != null && shadow.IsValid && shadow.Name == "Shadow" && shadow.IsAlly)
            {
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
            }
            var mark = sender as Obj_GeneralParticleEmitter;
            if (mark != null && mark.IsValid && rShadow.IsValid() && deathMark == null)
            {
                var markName = mark.Name.ToLower();
                if (markName.Contains(Player.ChampionName.ToLower()) && markName.Contains("base_r")
                    && markName.Contains("buf_tell"))
                {
                    deathMark = mark;
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
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    Orbwalk();
                    break;
                case OrbwalkingMode.Hybrid:
                    Hybrid();
                    break;
                case OrbwalkingMode.LastHit:
                    Farm();
                    break;
                case OrbwalkingMode.None:
                    if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                    {
                        Orbwalker.MoveOrder(Game.CursorPos);
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
            if (Orbwalker.ActiveMode != OrbwalkingMode.Combo && Orbwalker.ActiveMode != OrbwalkingMode.Hybrid)
            {
                AutoQ();
            }
        }

        private static void Orbwalk()
        {
            var target = GetTarget;
            if (target != null)
            {
                Swap(target);
                if (RState == 0 && MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName])
                {
                    R.CastOnUnit(target);
                }
                if (MainMenu["Orbwalk"]["Ignite"] && Ignite.IsReady()
                    && (HaveRMark(target) || target.HealthPercent < 30) && Player.Distance(target) < IgniteRange)
                {
                    Player.Spellbook.CastSpell(Ignite, target);
                }
                if (WState == 0
                    && ((Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost
                         && Player.Distance(target) < RangeW + Q.Range)
                        || (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                            && Player.Distance(target) < RangeW + E.Range)))
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
                        if (rShadow.IsValid() && MainMenu["Orbwalk"]["R"]
                            && MainMenu["Orbwalk"]["RCast" + target.ChampionName] && !HaveRMark(target))
                        {
                            CastW(target);
                        }
                    }
                    if (MainMenu["Orbwalk"]["WAdv"].GetValue<MenuList>().Index > 0 && rShadow.IsValid()
                        && MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                        && HaveRMark(target))
                    {
                        CastW(target, true);
                    }
                }
                if (Orbwalker.GetTarget(OrbwalkingMode.Combo) == null || (!Orbwalker.CanAttack && Orbwalker.CanMove))
                {
                    CastQ(target, true);
                }
                CastE();
            }
            if (MainMenu["Orbwalk"]["Item"])
            {
                UseItem(target);
            }
        }

        private static void Swap(Obj_AI_Hero target)
        {
            if (DeadByRMark(target))
            {
                return;
            }
            if (MainMenu["Orbwalk"]["SwapGap"].GetValue<MenuList>().Index > 0 && !target.InAutoAttackRange())
            {
                var distPlayer = Player.Distance(target);
                var distW = WState == 1 && wShadow.IsValid() ? wShadow.Distance(target) : 999999;
                var distR = RState == 1 && rShadow.IsValid() ? rShadow.Distance(target) : 999999;
                var minDist = Math.Min(Math.Min(distW, distR), distPlayer);
                if (minDist < distPlayer)
                {
                    switch (MainMenu["Orbwalk"]["SwapGap"].GetValue<MenuList>().Index)
                    {
                        case 1:
                            if (Math.Abs(minDist - distW) < float.Epsilon)
                            {
                                var calcCombo = GetComboDmg(
                                    target,
                                    Q.IsReady(),
                                    minDist < Q.Range || (rShadow.IsValid() && rShadow.Distance(target) < Q.Range),
                                    E.IsReady(),
                                    MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                                    && RState == 0);
                                if (target.Health + target.PhysicalShield < calcCombo[0]
                                    && (Player.Mana >= calcCombo[1] || Player.Mana * OverkillValue >= calcCombo[1]))
                                {
                                    W.Cast();
                                }
                                if (MainMenu["Orbwalk"]["R"] && MainMenu["Orbwalk"]["RCast" + target.ChampionName]
                                    && RState == 0 && !R.IsInRange(target) && minDist < R.Range)
                                {
                                    W.Cast();
                                }
                            }
                            else if (Math.Abs(minDist - distR) < float.Epsilon)
                            {
                                var calcCombo = GetComboDmg(
                                    target,
                                    Q.IsReady(),
                                    minDist < Q.Range || (wShadow.IsValid() && wShadow.Distance(target) < Q.Range),
                                    E.IsReady(),
                                    false);
                                if (target.Health + target.PhysicalShield < calcCombo[0]
                                    && (Player.Mana >= calcCombo[1] || Player.Mana * OverkillValue >= calcCombo[1]))
                                {
                                    R.Cast();
                                }
                                if (MainMenu["Orbwalk"]["WNormal"] && WState == 0 && distPlayer > RangeW - 100
                                    && !HaveRMark(target))
                                {
                                    if (Q.IsReady() && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost
                                        && minDist < RangeW + Q.Range)
                                    {
                                        R.Cast();
                                    }
                                    if (E.IsReady() && Player.Mana >= E.Instance.ManaCost + W.Instance.ManaCost
                                        && minDist < RangeW + E.Range)
                                    {
                                        R.Cast();
                                    }
                                }
                            }
                            break;
                        case 2:
                            if (minDist <= 500)
                            {
                                if (Math.Abs(minDist - distW) < float.Epsilon)
                                {
                                    W.Cast();
                                }
                                else if (Math.Abs(minDist - distR) < float.Epsilon)
                                {
                                    R.Cast();
                                }
                            }
                            break;
                    }
                }
            }
            if ((MainMenu["Orbwalk"]["SwapIfHpU"] > Player.HealthPercent && Player.HealthPercent < target.HealthPercent)
                || (MainMenu["Orbwalk"]["SwapIfKill"] && deathMark != null))
            {
                var countPlayer = Player.CountEnemyHeroesInRange(400);
                var countW = WState == 1 && wShadow.IsValid() ? wShadow.CountEnemyHeroesInRange(400) : 10;
                var countR = RState == 1 && rShadow.IsValid() ? rShadow.CountEnemyHeroesInRange(400) : 10;
                var minCount = Math.Min(countR, countW);
                if (minCount < countPlayer)
                {
                    if (minCount == countW)
                    {
                        W.Cast();
                    }
                    else if (minCount == countR)
                    {
                        R.Cast();
                    }
                }
            }
        }

        private static void TryEvading(List<Skillshot> hitBy, Vector2 to)
        {
            var dangerLevel = hitBy.Select(i => i.DangerLevel).Concat(new[] { 0 }).Max();
            var zedW =
                EvadeSpellDatabase.Spells.FirstOrDefault(
                    i => i.Enable && i.DangerLevel <= dangerLevel && i.IsReady && i.Slot == SpellSlot.W);
            if (zedW != null && Evade.IsAboutToHit(Player, zedW.Delay))
            {
                Player.Spellbook.CastSpell(zedW.Slot);
                return;
            }
            var zedR =
                EvadeSpellDatabase.Spells.FirstOrDefault(
                    i => i.Enable && i.DangerLevel <= dangerLevel && i.IsReady && i.Slot == SpellSlot.R);
            if (zedR != null && Evade.IsAboutToHit(Player, zedR.Delay))
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
    }
}